using Dapper;
using Joya.Pedidos.Api.Infraestrutura;

namespace Joya.Pedidos.Api.Features.Pedidos.ListarPedidos;

public sealed class ListarPedidosHandler(FabricaDeConexao fabricaDeConexao)
{
    // A página é recortada antes de calcular Itens e Valor Total: o agregado só roda para as linhas devolvidas,
    // coberto por IX_ItensPedido_PedidoId (ADR 0009).
    private const string SqlDaPagina = """
        WITH Pagina AS (
            SELECT p.Id, p.ClienteId, p.DataPedido
            FROM dbo.Pedidos AS p
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
        ORDER BY pg.DataPedido DESC, pg.Id DESC;
        """;

    private const string SqlDoTotal = "SELECT COUNT(*) FROM dbo.Pedidos AS p;";

    public async Task<ListarPedidosResponse> ExecutarAsync(int pagina, int tamanhoPagina, CancellationToken cancellationToken)
    {
        var parametros = new DynamicParameters();
        parametros.Add("Pular", (pagina - 1) * tamanhoPagina);
        parametros.Add("TamanhoPagina", tamanhoPagina);

        await using var conexao = await fabricaDeConexao.AbrirAsync(cancellationToken);
        var linhas = await conexao.QueryAsync<LinhaDePedido>(new CommandDefinition(SqlDaPagina, parametros, cancellationToken: cancellationToken));
        var totalItens = await conexao.ExecuteScalarAsync<int>(new CommandDefinition(SqlDoTotal, parametros, cancellationToken: cancellationToken));

        return new ListarPedidosResponse([.. linhas.Select(l => l.ParaResponse())], pagina, tamanhoPagina, totalItens);
    }

    private sealed record LinhaDePedido(int Id, int ClienteId, string NomeCliente, DateTime DataPedido, int QuantidadeItens, decimal ValorTotal)
    {
        // DATETIME2 volta sem Kind; a Data do Pedido é sempre UTC (RN08), então sai com "Z" no JSON.
        public PedidoListadoResponse ParaResponse() =>
            new(Id, ClienteId, NomeCliente, DateTime.SpecifyKind(DataPedido, DateTimeKind.Utc), QuantidadeItens, ValorTotal);
    }
}
