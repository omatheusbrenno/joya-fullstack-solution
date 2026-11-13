using System.Data;
using System.Text.Json;
using Dapper;
using Joya.Pedidos.Api.Infraestrutura;

namespace Joya.Pedidos.Api.Features.Pedidos.CriarPedido;

/// <summary>
/// Orquestra a criação do Pedido: carrega Cliente e Produtos, avalia as regras (função pura) e, numa transação,
/// faz a Baixa de Estoque com UPDATE condicional e grava o Pedido (ADR 0003). Qualquer falha desfaz tudo.
/// </summary>
public sealed partial class CriarPedidoHandler(FabricaDeConexao fabricaDeConexao, ILogger<CriarPedidoHandler> logger)
{
    // Os Itens viajam num único parâmetro JSON lido com OPENJSON: o número de parâmetros não cresce com a lista
    // e nenhum valor do request entra no texto do SQL (OWASP A05).
    private const string SqlDeCarga = """
        SELECT Id, Nome FROM dbo.Clientes WHERE Id = @ClienteId;
        SELECT p.Id, p.Nome, p.Preco, p.Estoque
        FROM dbo.Produtos AS p
        WHERE p.Id IN (SELECT i.ProdutoId FROM OPENJSON(@Itens) WITH (ProdutoId INT) AS i);
        """;

    // Baixa de Estoque atômica (RN09): só baixa quem ainda tem Estoque. Os Produtos devolvidos pelo OUTPUT
    // são os que baixaram; faltar algum significa que outro Pedido levou as unidades entre a leitura e aqui.
    private const string SqlDaBaixaDeEstoque = """
        UPDATE p
        SET p.Estoque = p.Estoque - i.Quantidade
        OUTPUT INSERTED.Id
        FROM dbo.Produtos AS p
        INNER JOIN OPENJSON(@Itens) WITH (ProdutoId INT, Quantidade INT) AS i ON i.ProdutoId = p.Id
        WHERE p.Estoque >= i.Quantidade;
        """;

    // A Data do Pedido é definida pelo servidor, em UTC (RN08).
    private const string SqlDoPedido = """
        INSERT INTO dbo.Pedidos (ClienteId, DataPedido)
        OUTPUT INSERTED.Id, INSERTED.DataPedido
        VALUES (@ClienteId, SYSUTCDATETIME());
        """;

    private const string SqlDosItens = """
        INSERT INTO dbo.ItensPedido (PedidoId, ProdutoId, Quantidade, PrecoUnitario)
        SELECT @PedidoId, i.ProdutoId, i.Quantidade, i.PrecoUnitario
        FROM OPENJSON(@Itens) WITH (ProdutoId INT, Quantidade INT, PrecoUnitario DECIMAL(18, 2)) AS i;
        """;

    public async Task<ResultadoDeCriarPedido> ExecutarAsync(CriarPedidoRequest request, CancellationToken cancellationToken)
    {
        var itens = request.ItensSolicitados();

        await using var conexao = await fabricaDeConexao.AbrirAsync(cancellationToken);
        await using var transacao = await conexao.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

        ClienteEncontrado? cliente;
        List<ProdutoEncontrado> produtos;
        await using (var carga = await conexao.QueryMultipleAsync(Comando(SqlDeCarga, new { request.ClienteId, Itens = Json(itens) }, transacao, cancellationToken)))
        {
            cliente = await carga.ReadSingleOrDefaultAsync<ClienteEncontrado>();
            produtos = [.. await carga.ReadAsync<ProdutoEncontrado>()];
        }

        var avaliacao = RegrasDoPedido.Avaliar(itens, cliente, produtos);
        if (avaliacao is not PedidoAceito { Pedido: var pedido })
        {
            return new PedidoNaoCriado(((PedidoRecusado)avaliacao).Violacoes);
        }

        var itensJson = Json(pedido.Itens);
        var produtosBaixados = (await conexao.QueryAsync<int>(Comando(SqlDaBaixaDeEstoque, new { Itens = itensJson }, transacao, cancellationToken))).ToHashSet();
        if (produtosBaixados.Count < pedido.Itens.Count)
        {
            await transacao.RollbackAsync(cancellationToken);
            LogEstoqueConsumido(logger, pedido.Itens.Count - produtosBaixados.Count);
            return new PedidoNaoCriado(RegrasDoPedido.EstoqueConsumidoNaBaixa(pedido, produtosBaixados));
        }

        var (pedidoId, dataPedido) = await conexao.QuerySingleAsync<(int, DateTime)>(Comando(SqlDoPedido, new { ClienteId = pedido.Cliente.Id }, transacao, cancellationToken));
        await conexao.ExecuteAsync(Comando(SqlDosItens, new { PedidoId = pedidoId, Itens = itensJson }, transacao, cancellationToken));
        await transacao.CommitAsync(cancellationToken);

        LogPedidoCriado(logger, pedidoId, pedido.Itens.Count);
        return new PedidoCriado(ParaResponse(pedidoId, dataPedido, pedido));
    }

    private static CriarPedidoResponse ParaResponse(int id, DateTime dataPedido, PedidoMontado pedido) =>
        new(
            id,
            pedido.Cliente.Id,
            pedido.Cliente.Nome,
            // DATETIME2 volta sem Kind. A Data do Pedido é sempre UTC (RN08), então sai com "Z" no JSON.
            DateTime.SpecifyKind(dataPedido, DateTimeKind.Utc),
            [.. pedido.Itens.Select(item => new ItemDoPedidoResponse(item.ProdutoId, item.NomeProduto, item.Quantidade, item.PrecoUnitario))],
            pedido.ValorTotal);

    private static string Json<T>(T valor) => JsonSerializer.Serialize(valor);

    private static CommandDefinition Comando(string sql, object parametros, IDbTransaction transacao, CancellationToken cancellationToken) =>
        new(sql, parametros, transacao, cancellationToken: cancellationToken);

    [LoggerMessage(Level = LogLevel.Information, Message = "Pedido {PedidoId} criado com {QuantidadeItens} Itens.")]
    private static partial void LogPedidoCriado(ILogger logger, int pedidoId, int quantidadeItens);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Pedido recusado na Baixa de Estoque: {QuantidadeItens} Itens sem Estoque após a leitura.")]
    private static partial void LogEstoqueConsumido(ILogger logger, int quantidadeItens);
}
