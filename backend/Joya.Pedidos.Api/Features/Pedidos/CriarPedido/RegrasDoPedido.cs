namespace Joya.Pedidos.Api.Features.Pedidos.CriarPedido;

/// <summary>
/// Regras do Pedido (RN01 a RN07, RN10, RN11) numa função pura: recebe o request e o que foi carregado
/// do banco e devolve o Pedido montado ou as violações. Sem I/O e sem exceção para fluxo esperado (ADR 0002).
/// </summary>
public static class RegrasDoPedido
{
    public static AvaliacaoDoPedido Avaliar(
        IReadOnlyList<ItemSolicitado> itens,
        ClienteEncontrado? cliente,
        IReadOnlyCollection<ProdutoEncontrado> produtos)
    {
        var produtosPorId = produtos.ToDictionary(produto => produto.Id);
        var itensMontados = itens
            .Select(item =>
            {
                var produto = produtosPorId[item.ProdutoId];
                return new ItemMontado(produto.Id, produto.Nome, item.Quantidade, produto.Preco);
            })
            .ToList();
        return new PedidoAceito(new PedidoMontado(cliente!, itensMontados));
    }
}
