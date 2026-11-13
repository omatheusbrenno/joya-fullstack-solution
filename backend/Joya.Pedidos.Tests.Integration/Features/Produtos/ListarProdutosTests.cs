using System.Net;
using System.Net.Http.Json;
using Joya.Pedidos.Tests.Integration.Infraestrutura;

namespace Joya.Pedidos.Tests.Integration.Features.Produtos;

[Collection(ColecaoDoBanco.Nome)]
public sealed class ListarProdutosTests(BancoDeTestes banco) : IAsyncLifetime
{
    private readonly CenarioDoBanco _cenario = new(banco);

    public async ValueTask InitializeAsync() => await _cenario.LimparAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task Produtos_devolve_id_nome_preco_e_Estoque_de_todos_ordenados_por_nome()
    {
        var teclado = await _cenario.InserirProdutoAsync("Teclado", 150.00m, estoque: 12);
        var webcam = await _cenario.InserirProdutoAsync("Webcam", 279.90m, estoque: 0);
        var mouse = await _cenario.InserirProdutoAsync("Mouse", 79.90m, estoque: 200);
        await using var api = new ApiDeTestes(banco.ConnectionString);
        using var cliente = api.CreateClient();

        var resposta = await cliente.GetAsync(new Uri("/produtos", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var produtos = await resposta.Content.ReadFromJsonAsync<ProdutoLido[]>(TestContext.Current.CancellationToken) ?? [];
        Assert.Equal(
            [new ProdutoLido(mouse, "Mouse", 79.90m, 200), new ProdutoLido(teclado, "Teclado", 150.00m, 12), new ProdutoLido(webcam, "Webcam", 279.90m, 0)],
            produtos);
    }

    private sealed record ProdutoLido(int Id, string Nome, decimal Preco, int Estoque);
}
