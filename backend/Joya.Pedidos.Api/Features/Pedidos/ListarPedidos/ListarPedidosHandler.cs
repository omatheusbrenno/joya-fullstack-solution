using System.Data;
using Dapper;
using Joya.Pedidos.Api.Infraestrutura;

namespace Joya.Pedidos.Api.Features.Pedidos.ListarPedidos;

public sealed class ListarPedidosHandler(FabricaDeConexao fabricaDeConexao)
{
    // Filtros opcionais com parâmetros fixos: nada de SQL montado com entrada do usuário (OWASP A05).
    // O Período vira o intervalo semiaberto [@Inicio, @FimExclusivo), sem função sobre DataPedido.
    // OPTION (RECOMPILE) deixa o otimizador descartar os filtros nulos e usar os índices de Pedidos.
    private const string Filtros = """
        WHERE (@ClienteId IS NULL OR p.ClienteId = @ClienteId)
          AND (@Inicio IS NULL OR p.DataPedido >= @Inicio)
          AND (@FimExclusivo IS NULL OR p.DataPedido < @FimExclusivo)
        """;

    // A página é recortada antes de calcular Itens e Valor Total: o agregado só roda para as linhas devolvidas,
    // coberto por IX_ItensPedido_PedidoId (ADR 0009).
    private const string SqlDaPagina = $"""
        WITH Pagina AS (
            SELECT p.Id, p.ClienteId, p.DataPedido
            FROM dbo.Pedidos AS p
            {Filtros}
            ORDER BY p.DataPedido DESC, p.Id DESC
            OFFSET @Pular ROWS FETCH NEXT @TamanhoPagina ROWS ONLY
        )
        SELECT pg.Id, pg.ClienteId, c.Nome AS NomeCliente, pg.DataPedido, i.QuantidadeItens, i.ValorTotal
        FROM Pagina AS pg
        INNER JOIN dbo.Clientes AS c ON c.Id = pg.ClienteId
        CROSS APPLY (
            SELECT COUNT(*) AS QuantidadeItens, COALESCE(SUM(ip.Quantidade * ip.PrecoUnitario), 0) AS ValorTotal
            FROM dbo.ItensPedido AS ip
            WHERE ip.PedidoId = pg.Id
        ) AS i
        ORDER BY pg.DataPedido DESC, pg.Id DESC
        OPTION (RECOMPILE);
        """;

    private const string SqlDoTotal = $"""
        SELECT COUNT(*)
        FROM dbo.Pedidos AS p
        {Filtros}
        OPTION (RECOMPILE);
        """;

    public async Task<ListarPedidosResponse> ExecutarAsync(ListarPedidosRequest request, CancellationToken cancellationToken)
    {
        var parametros = new DynamicParameters();
        parametros.Add("ClienteId", request.ClienteId, DbType.Int32);
        parametros.Add("Inicio", request.DataInicio?.Valor.ToDateTime(TimeOnly.MinValue), DbType.DateTime2);
        parametros.Add("FimExclusivo", FimExclusivo(request.DataFim?.Valor), DbType.DateTime2);
        // long: uma página muito alta não estoura o int (OFFSET aceita bigint).
        parametros.Add("Pular", (long)(request.Pagina - 1) * request.TamanhoPagina, DbType.Int64);
        parametros.Add("TamanhoPagina", request.TamanhoPagina, DbType.Int32);

        await using var conexao = await fabricaDeConexao.AbrirAsync(cancellationToken);
        var linhas = await conexao.QueryAsync<LinhaDePedido>(new CommandDefinition(SqlDaPagina, parametros, cancellationToken: cancellationToken));
        var totalItens = await conexao.ExecuteScalarAsync<int>(new CommandDefinition(SqlDoTotal, parametros, cancellationToken: cancellationToken));

        return new ListarPedidosResponse([.. linhas.Select(l => l.ParaResponse())], request.Pagina, request.TamanhoPagina, totalItens);
    }

    /// <summary>Meia-noite do dia seguinte à data final: a data final entra inteira, até 23:59:59.</summary>
    private static DateTime? FimExclusivo(DateOnly? dataFim) =>
        dataFim is null || dataFim == DateOnly.MaxValue ? null : dataFim.Value.AddDays(1).ToDateTime(TimeOnly.MinValue);

    private sealed record LinhaDePedido(int Id, int ClienteId, string NomeCliente, DateTime DataPedido, int QuantidadeItens, decimal ValorTotal)
    {
        // DATETIME2 volta sem Kind. A Data do Pedido é sempre UTC (RN08), então sai com "Z" no JSON.
        public PedidoListadoResponse ParaResponse() =>
            new(Id, ClienteId, NomeCliente, DateTime.SpecifyKind(DataPedido, DateTimeKind.Utc), QuantidadeItens, ValorTotal);
    }
}
