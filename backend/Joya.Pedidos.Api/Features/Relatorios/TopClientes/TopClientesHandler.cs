using System.Data;
using Dapper;
using Joya.Pedidos.Api.Infraestrutura;

namespace Joya.Pedidos.Api.Features.Relatorios.TopClientes;

public sealed class TopClientesHandler(FabricaDeConexao fabricaDeConexao)
{
    public const int QuantidadeDeClientes = 5;

    // Agrupa pelo id do Cliente, nunca pelo nome: homônimos são Clientes distintos (RN13).
    // O Período é o intervalo semiaberto [@Inicio, @FimExclusivo), sem função sobre DataPedido, para usar o índice.
    // O Valor Total de cada Pedido vem dos Itens (ADR 0009); o desempate pelo id deixa a ordem estável.
    // Só valores entram pelos parâmetros: nenhuma entrada do usuário compõe o texto do SQL (OWASP A05).
    private const string Sql = """
        WITH ValorDoPedido AS (
            SELECT p.ClienteId, v.ValorTotal
            FROM dbo.Pedidos AS p
            CROSS APPLY (
                SELECT COALESCE(SUM(ip.Quantidade * ip.PrecoUnitario), 0) AS ValorTotal
                FROM dbo.ItensPedido AS ip
                WHERE ip.PedidoId = p.Id
            ) AS v
            WHERE p.DataPedido >= @Inicio
              AND p.DataPedido < @FimExclusivo
        ),
        PorCliente AS (
            SELECT ClienteId, COUNT(*) AS QuantidadePedidos, SUM(ValorTotal) AS ValorTotal
            FROM ValorDoPedido
            GROUP BY ClienteId
        )
        SELECT TOP (@Quantidade) pc.ClienteId, c.Nome, pc.QuantidadePedidos, pc.ValorTotal
        FROM PorCliente AS pc
        INNER JOIN dbo.Clientes AS c ON c.Id = pc.ClienteId
        ORDER BY pc.ValorTotal DESC, pc.ClienteId ASC;
        """;

    public async Task<IReadOnlyList<TopClienteResponse>> ExecutarAsync(TopClientesRequest request, CancellationToken cancellationToken)
    {
        // O AddValidation já recusou (400) o request sem dataInicio ou dataFim.
        var dataInicio = request.DataInicio!.Value.Valor;
        var dataFim = request.DataFim!.Value.Valor;

        var parametros = new DynamicParameters();
        parametros.Add("Inicio", dataInicio.ToDateTime(TimeOnly.MinValue), DbType.DateTime2);
        parametros.Add("FimExclusivo", FimExclusivo(dataFim), DbType.DateTime2);
        parametros.Add("Quantidade", QuantidadeDeClientes, DbType.Int32);

        await using var conexao = await fabricaDeConexao.AbrirAsync(cancellationToken);
        var linhas = await conexao.QueryAsync<TopClienteResponse>(new CommandDefinition(Sql, parametros, cancellationToken: cancellationToken));
        return [.. linhas];
    }

    /// <summary>Meia-noite do dia seguinte à data final: a data final entra inteira, até 23:59:59.</summary>
    private static DateTime FimExclusivo(DateOnly dataFim) =>
        dataFim == DateOnly.MaxValue ? DateTime.MaxValue : dataFim.AddDays(1).ToDateTime(TimeOnly.MinValue);
}
