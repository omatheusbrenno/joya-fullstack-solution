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

    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    public void Pedido_sem_Itens_ou_com_mais_de_50_e_recusado_por_forma(int quantidadeDeItens)
    {
        var produtos = Enumerable.Range(1, quantidadeDeItens).Select(id => new ProdutoEncontrado(id, $"Produto {id}", 1m, 10)).ToArray();
        var itens = produtos.Select(produto => new ItemSolicitado(produto.Id, 1)).ToArray();

        var violacoes = Violacoes(RegrasDoPedido.Avaliar(itens, Ana, produtos));

        Assert.Equal([new Violacao(TipoDeViolacao.Forma, "itens", "O Pedido deve ter de 1 a 50 Itens.")], violacoes);
    }

    [Fact]
    public void Pedido_com_exatamente_50_Itens_e_aceito()
    {
        var produtos = Enumerable.Range(1, 50).Select(id => new ProdutoEncontrado(id, $"Produto {id}", 1m, 10)).ToArray();

        var avaliacao = RegrasDoPedido.Avaliar([.. produtos.Select(produto => new ItemSolicitado(produto.Id, 1))], Ana, produtos);

        Assert.IsType<PedidoAceito>(avaliacao);
    }

    [Fact]
    public void Quantidade_zero_negativa_ou_acima_de_10000_e_recusada_por_forma_apontando_cada_Item()
    {
        var produtos = Enumerable.Range(1, 5).Select(id => new ProdutoEncontrado(id, $"Produto {id}", 1m, 20_000)).ToArray();

        var violacoes = Violacoes(RegrasDoPedido.Avaliar(
            [new ItemSolicitado(1, 0), new ItemSolicitado(2, 1), new ItemSolicitado(3, -5), new ItemSolicitado(4, 10_000), new ItemSolicitado(5, 10_001)],
            Ana,
            produtos));

        const string Mensagem = "A quantidade deve estar entre 1 e 10000.";
        Assert.Equal(
            [
                new Violacao(TipoDeViolacao.Forma, "itens[0].quantidade", Mensagem),
                new Violacao(TipoDeViolacao.Forma, "itens[2].quantidade", Mensagem),
                new Violacao(TipoDeViolacao.Forma, "itens[4].quantidade", Mensagem),
            ],
            violacoes);
    }

    [Fact]
    public void Produto_repetido_e_recusado_por_forma_apontando_cada_repeticao_junto_com_as_outras_violacoes()
    {
        var violacoes = Violacoes(RegrasDoPedido.Avaliar(
            [new ItemSolicitado(3, 1), new ItemSolicitado(4, 0), new ItemSolicitado(3, 2), new ItemSolicitado(3, 1)],
            Ana,
            [Teclado, Mouse]));

        Assert.Equal(
            [
                new Violacao(TipoDeViolacao.Forma, "itens[1].quantidade", "A quantidade deve estar entre 1 e 10000."),
                new Violacao(TipoDeViolacao.Forma, "itens[2].produtoId", "O Produto 3 já aparece no Item 1."),
                new Violacao(TipoDeViolacao.Forma, "itens[3].produtoId", "O Produto 3 já aparece no Item 1."),
            ],
            violacoes);
    }

    private static IReadOnlyList<Violacao> Violacoes(AvaliacaoDoPedido avaliacao) =>
        Assert.IsType<PedidoRecusado>(avaliacao).Violacoes;
}
