using System.Net;
using System.Net.Http.Json;
using Joya.Pedidos.Tests.Integration.Infraestrutura;

namespace Joya.Pedidos.Tests.Integration.Features.Clientes;

[Collection(ColecaoDoBanco.Nome)]
public sealed class ListarClientesTests(BancoDeTestes banco) : IAsyncLifetime
{
    private readonly CenarioDoBanco _cenario = new(banco);

    public async ValueTask InitializeAsync() => await _cenario.LimparAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task Clientes_devolve_id_e_nome_de_todos_incluindo_homonimos()
    {
        var anaA = await _cenario.InserirClienteAsync("Ana Souza");
        var bruno = await _cenario.InserirClienteAsync("Bruno Lima");
        var anaB = await _cenario.InserirClienteAsync("Ana Souza");
        await using var api = new ApiDeTestes(banco.ConnectionString);
        using var cliente = api.CreateClient();

        var resposta = await cliente.GetAsync(new Uri("/clientes", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var clientes = await resposta.Content.ReadFromJsonAsync<ClienteLido[]>(TestContext.Current.CancellationToken) ?? [];
        Assert.Equal(
            [new ClienteLido(anaA, "Ana Souza"), new ClienteLido(anaB, "Ana Souza"), new ClienteLido(bruno, "Bruno Lima")],
            clientes);
    }

    private sealed record ClienteLido(int Id, string Nome);
}
