using Joya.Pedidos.Api.Features.Pedidos.CriarPedido;

namespace Joya.Pedidos.Tests.Unit.Features.Pedidos.CriarPedido;

public sealed class RegrasDoPedidoTests
{
    private static readonly ClienteEncontrado Ana = new(1, "Ana Souza");
    private static readonly ProdutoEncontrado Teclado = new(3, "Teclado", 150.00m, 10);
    private static readonly ProdutoEncontrado Mouse = new(4, "Mouse", 79.90m, 5);

    [Fact]
    public void Pedido_valido_congela_o_Preco_Unitario_de_cada_Produto_e_calcula_o_Valor_Total()
    {
        var avaliacao = RegrasDoPedido.Avaliar(
            [new ItemSolicitado(3, 2), new ItemSolicitado(4, 3)],
            Ana,
            [Teclado, Mouse]);

        var pedido = Assert.IsType<PedidoAceito>(avaliacao).Pedido;
        Assert.Equal(Ana, pedido.Cliente);
        Assert.Equal(
            [new ItemMontado(3, "Teclado", 2, 150.00m), new ItemMontado(4, "Mouse", 3, 79.90m)],
            pedido.Itens);
        Assert.Equal(539.70m, pedido.ValorTotal);
    }
}
