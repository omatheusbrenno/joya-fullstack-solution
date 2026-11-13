using System.Net.Http.Json;
using Joya.Pedidos.Tests.Integration.Infraestrutura;

namespace Joya.Pedidos.Tests.Integration.Features.Carga;

/// <summary>A carga legível de database/ roda em todo docker compose up: precisa ser idempotente.</summary>
[Collection(ColecaoDoBanco.Nome)]
public sealed class CargaDeExemploTests(BancoDeTestes banco) : IAsyncLifetime
{
    private const string ScriptDaCarga = "03-carga-exemplo.sql";

    private readonly CenarioDoBanco _cenario = new(banco);

    public async ValueTask InitializeAsync() => await _cenario.LimparAsync();

    public async ValueTask DisposeAsync() => await _cenario.LimparAsync();

    [Fact]
    public async Task Carga_aplicada_duas_vezes_nao_duplica_e_traz_homonimos_e_Pedidos_listaveis()
    {
        await banco.AplicarScriptAsync(ScriptDaCarga);
        await banco.AplicarScriptAsync(ScriptDaCarga);
        await using var api = new ApiDeTestes(banco.ConnectionString);
        using var cliente = api.CreateClient();
        var cancelamento = TestContext.Current.CancellationToken;

        var clientes = await cliente.GetFromJsonAsync<ClienteLido[]>(new Uri("/clientes", UriKind.Relative), cancelamento) ?? [];
        var pedidos = await cliente.GetFromJsonAsync<PaginaDePedidos>(new Uri("/pedidos?tamanhoPagina=100", UriKind.Relative), cancelamento);

        Assert.Equal(10, clientes.Length);
        Assert.Equal(2, clientes.Count(c => c.Nome == "Ana Souza"));
        Assert.Equal(12, pedidos?.TotalItens);
        Assert.All(pedidos?.Itens ?? [], p => Assert.True(p.ValorTotal > 0));
    }

    private sealed record ClienteLido(int Id, string Nome);

    private sealed record PedidoListado(int Id, decimal ValorTotal);

    private sealed record PaginaDePedidos(PedidoListado[] Itens, int TotalItens);
}
