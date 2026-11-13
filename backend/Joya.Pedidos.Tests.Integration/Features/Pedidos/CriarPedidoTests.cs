using System.Net;
using System.Net.Http.Json;
using Joya.Pedidos.Tests.Integration.Infraestrutura;

namespace Joya.Pedidos.Tests.Integration.Features.Pedidos;

[Collection(ColecaoDoBanco.Nome)]
public sealed class CriarPedidoTests(BancoDeTestes banco) : IAsyncLifetime
{
    private readonly CenarioDoBanco _cenario = new(banco);
    private ApiDeTestes _api = null!;
    private HttpClient _cliente = null!;

    public async ValueTask InitializeAsync()
    {
        await _cenario.LimparAsync();
        _api = new ApiDeTestes(banco.ConnectionString);
        _cliente = _api.CreateClient();
    }

    public async ValueTask DisposeAsync()
    {
        _cliente.Dispose();
        await _api.DisposeAsync();
    }

    [Fact]
    public async Task Pedido_valido_devolve_201_com_Preco_Unitario_atual_Valor_Total_e_Data_UTC_e_baixa_o_Estoque()
    {
        var ana = await _cenario.InserirClienteAsync("Ana Souza");
        var teclado = await _cenario.InserirProdutoAsync("Teclado", 150.00m, estoque: 10);
        var mouse = await _cenario.InserirProdutoAsync("Mouse", 79.90m, estoque: 5);
        var antes = DateTime.UtcNow.AddSeconds(-1);

        // O "precoUnitario" do request é ignorado (CA09): vale o preço do Produto.
        var resposta = await CriarAsync(new
        {
            clienteId = ana,
            itens = new object[]
            {
                new { produtoId = teclado, quantidade = 2, precoUnitario = 0.01m },
                new { produtoId = mouse, quantidade = 3 },
            },
        });

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        var pedido = await resposta.Content.ReadFromJsonAsync<PedidoCriado>(TestContext.Current.CancellationToken)
            ?? throw new InvalidOperationException("Resposta vazia.");
        Assert.Equal((ana, "Ana Souza"), (pedido.ClienteId, pedido.NomeCliente));
        Assert.Equal([new ItemCriado(teclado, "Teclado", 2, 150.00m), new ItemCriado(mouse, "Mouse", 3, 79.90m)], pedido.Itens);
        Assert.Equal(539.70m, pedido.ValorTotal);
        Assert.Equal(DateTimeKind.Utc, pedido.DataPedido.Kind);
        Assert.InRange(pedido.DataPedido, antes, DateTime.UtcNow.AddSeconds(1));

        Assert.Equal(new Dictionary<int, int> { [teclado] = 8, [mouse] = 2 }, await EstoquesAsync());
        var listado = Assert.Single((await ListarAsync()).Itens);
        Assert.Equal((pedido.Id, pedido.DataPedido, 2, 539.70m), (listado.Id, listado.DataPedido, listado.QuantidadeItens, listado.ValorTotal));
    }

    [Fact]
    public async Task Cliente_inexistente_devolve_422_apontando_o_Cliente_sem_persistir_nem_baixar_Estoque()
    {
        var teclado = await _cenario.InserirProdutoAsync("Teclado", 150.00m, estoque: 10);

        var resposta = await CriarAsync(new { clienteId = 999_999, itens = new[] { new { produtoId = teclado, quantidade = 1 } } });

        var erros = await ProblemaAsync(resposta, HttpStatusCode.UnprocessableEntity);
        Assert.Equal(["clienteId"], erros.Keys);
        await AssertNadaMudouAsync(new Dictionary<int, int> { [teclado] = 10 });
    }

    [Theory]
    [InlineData(0, "itens")]
    [InlineData(51, "itens")]
    public async Task Pedido_sem_Itens_ou_com_mais_de_50_devolve_400(int quantidadeDeItens, string campo)
    {
        var ana = await _cenario.InserirClienteAsync("Ana Souza");
        var produtos = new List<int>();
        for (var i = 0; i < quantidadeDeItens; i++)
        {
            produtos.Add(await _cenario.InserirProdutoAsync($"Produto {i}", 1m));
        }

        var resposta = await CriarAsync(new { clienteId = ana, itens = produtos.Select(produtoId => new { produtoId, quantidade = 1 }) });

        Assert.Equal([campo], (await ProblemaAsync(resposta, HttpStatusCode.BadRequest)).Keys);
    }

    [Fact]
    public async Task Pedido_sem_o_campo_itens_devolve_400()
    {
        var ana = await _cenario.InserirClienteAsync("Ana Souza");

        var resposta = await CriarAsync(new { clienteId = ana });

        Assert.Equal(["itens"], (await ProblemaAsync(resposta, HttpStatusCode.BadRequest)).Keys);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10_001)]
    public async Task Quantidade_fora_de_1_a_10000_devolve_400_indicando_o_Item(int quantidade)
    {
        var ana = await _cenario.InserirClienteAsync("Ana Souza");
        var teclado = await _cenario.InserirProdutoAsync("Teclado", 150.00m, estoque: 20_000);
        var mouse = await _cenario.InserirProdutoAsync("Mouse", 79.90m);

        var resposta = await CriarAsync(new { clienteId = ana, itens = new[] { new { produtoId = mouse, quantidade = 1 }, new { produtoId = teclado, quantidade } } });

        Assert.Equal(["itens[1].quantidade"], (await ProblemaAsync(resposta, HttpStatusCode.BadRequest)).Keys);
        await AssertNadaMudouAsync(new Dictionary<int, int> { [teclado] = 20_000, [mouse] = 100 });
    }

    [Fact]
    public async Task Produto_repetido_devolve_400()
    {
        var ana = await _cenario.InserirClienteAsync("Ana Souza");
        var teclado = await _cenario.InserirProdutoAsync("Teclado", 150.00m);

        var resposta = await CriarAsync(new { clienteId = ana, itens = new[] { new { produtoId = teclado, quantidade = 1 }, new { produtoId = teclado, quantidade = 2 } } });

        Assert.Equal(["itens[1].produtoId"], (await ProblemaAsync(resposta, HttpStatusCode.BadRequest)).Keys);
    }

    [Fact]
    public async Task Produto_inexistente_devolve_422_indicando_o_Item()
    {
        var ana = await _cenario.InserirClienteAsync("Ana Souza");
        var teclado = await _cenario.InserirProdutoAsync("Teclado", 150.00m, estoque: 10);

        var resposta = await CriarAsync(new { clienteId = ana, itens = new[] { new { produtoId = teclado, quantidade = 1 }, new { produtoId = 999_999, quantidade = 1 } } });

        Assert.Equal(["itens[1].produtoId"], (await ProblemaAsync(resposta, HttpStatusCode.UnprocessableEntity)).Keys);
        await AssertNadaMudouAsync(new Dictionary<int, int> { [teclado] = 10 });
    }

    [Fact]
    public async Task Estoque_insuficiente_em_dois_Itens_devolve_422_listando_os_dois_sem_persistir_nada()
    {
        var ana = await _cenario.InserirClienteAsync("Ana Souza");
        var teclado = await _cenario.InserirProdutoAsync("Teclado", 150.00m, estoque: 1);
        var mouse = await _cenario.InserirProdutoAsync("Mouse", 79.90m, estoque: 10);
        var webcam = await _cenario.InserirProdutoAsync("Webcam", 279.90m, estoque: 0);

        var resposta = await CriarAsync(new
        {
            clienteId = ana,
            itens = new[] { new { produtoId = teclado, quantidade = 2 }, new { produtoId = mouse, quantidade = 3 }, new { produtoId = webcam, quantidade = 1 } },
        });

        var erros = await ProblemaAsync(resposta, HttpStatusCode.UnprocessableEntity);
        Assert.Equal(["itens[0].quantidade", "itens[2].quantidade"], erros.Keys.Order(StringComparer.Ordinal));
        await AssertNadaMudouAsync(new Dictionary<int, int> { [teclado] = 1, [mouse] = 10, [webcam] = 0 });
    }

    private async Task AssertNadaMudouAsync(Dictionary<int, int> estoquesEsperados)
    {
        Assert.Equal(estoquesEsperados, await EstoquesAsync());
        Assert.Equal(0, (await ListarAsync()).TotalItens);
    }

    private static async Task<Dictionary<string, string[]>> ProblemaAsync(HttpResponseMessage resposta, HttpStatusCode statusEsperado)
    {
        Assert.Equal(statusEsperado, resposta.StatusCode);
        Assert.Equal("application/problem+json", resposta.Content.Headers.ContentType?.MediaType);
        var problema = await resposta.Content.ReadFromJsonAsync<Problema>(TestContext.Current.CancellationToken)
            ?? throw new InvalidOperationException("Resposta vazia.");
        Assert.Equal((int)statusEsperado, problema.Status);
        Assert.All(problema.Errors.Values, mensagens => Assert.NotEmpty(mensagens));
        return problema.Errors;
    }

    private async Task<HttpResponseMessage> CriarAsync(object corpo) =>
        await _cliente.PostAsJsonAsync(new Uri("/pedidos", UriKind.Relative), corpo, TestContext.Current.CancellationToken);

    private async Task<Dictionary<int, int>> EstoquesAsync()
    {
        var produtos = await _cliente.GetFromJsonAsync<ProdutoLido[]>(new Uri("/produtos", UriKind.Relative), TestContext.Current.CancellationToken) ?? [];
        return produtos.ToDictionary(produto => produto.Id, produto => produto.Estoque);
    }

    private async Task<PaginaDePedidos> ListarAsync() =>
        await _cliente.GetFromJsonAsync<PaginaDePedidos>(new Uri("/pedidos", UriKind.Relative), TestContext.Current.CancellationToken)
        ?? throw new InvalidOperationException("Resposta vazia.");

    private sealed record ItemCriado(int ProdutoId, string NomeProduto, int Quantidade, decimal PrecoUnitario);

    private sealed record PedidoCriado(int Id, int ClienteId, string NomeCliente, DateTime DataPedido, ItemCriado[] Itens, decimal ValorTotal);

    private sealed record ProdutoLido(int Id, int Estoque);

    private sealed record Problema(int Status, Dictionary<string, string[]> Errors);

    private sealed record PedidoListado(int Id, DateTime DataPedido, int QuantidadeItens, decimal ValorTotal);

    private sealed record PaginaDePedidos(PedidoListado[] Itens, int TotalItens);
}
