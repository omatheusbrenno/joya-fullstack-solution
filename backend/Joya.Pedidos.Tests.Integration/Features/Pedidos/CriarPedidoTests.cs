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

    private sealed record PedidoListado(int Id, DateTime DataPedido, int QuantidadeItens, decimal ValorTotal);

    private sealed record PaginaDePedidos(PedidoListado[] Itens, int TotalItens);
}
