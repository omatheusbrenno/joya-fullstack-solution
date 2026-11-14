using System.Net.Http.Json;
using Joya.Pedidos.Tests.Integration.Infraestrutura;

namespace Joya.Pedidos.Tests.Integration.Features.Carga;

/// <summary>
/// O volume de database/ é determinístico: gerado do número da linha, sem NEWID() nem RAND().
/// Os valores esperados são literais: qualquer banco novo com a carga precisa devolver exatamente estes números.
/// </summary>
[Collection(ColecaoDoBanco.Nome)]
public sealed class CargaDeVolumeTests(BancoDeTestes banco) : IAsyncLifetime
{
    private const int PedidosDaCargaLegivel = 12;
    private const int PedidosDoVolume = 50_000;

    private readonly CenarioDoBanco _cenario = new(banco);

    public async ValueTask InitializeAsync() => await _cenario.LimparAsync();

    public async ValueTask DisposeAsync() => await _cenario.LimparAsync();

    [Fact]
    public async Task Volume_aplicado_duas_vezes_nao_duplica_e_produz_sempre_o_mesmo_Top_Clientes()
    {
        await banco.AplicarScriptAsync("03-carga-exemplo.sql");
        await banco.AplicarScriptAsync("04-carga-volume.sql");
        await banco.AplicarScriptAsync("04-carga-volume.sql");
        await using var api = new ApiDeTestes(banco.ConnectionString);
        using var cliente = api.CreateClient();

        var pedidos = await LerAsync<PaginaDePedidos>(cliente, "/pedidos?tamanhoPagina=1");
        var top2025 = await LerAsync<TopCliente[]>(cliente, "/relatorios/top-clientes?dataInicio=2025-01-01&dataFim=2025-12-31");

        Assert.Equal(PedidosDaCargaLegivel + PedidosDoVolume, pedidos.TotalItens);
        // Literais conferidos por um cálculo independente das fórmulas documentadas em 04-carga-volume.sql,
        // somados aos Pedidos de 2025 da carga legível. O E2E do Top Clientes usa os mesmos números.
        Assert.Equal(
            [
                new TopCliente(1, "Ana Souza", 6027, 22_005_966.90m),
                new TopCliente(4, "Ana Souza", 5272, 19_267_052.90m),
                new TopCliente(7, "Carlos Lima", 4520, 16_517_209.20m),
                new TopCliente(2, "Bruno Lima", 3771, 13_787_821.70m),
                new TopCliente(3, "Carla Mendes", 3013, 10_990_530.80m),
            ],
            top2025);
    }

    [Theory]
    [InlineData(9)]
    [InlineData(10)]
    public async Task Volume_preserva_os_Clientes_sem_Pedidos_em_2026(int clienteId)
    {
        await banco.AplicarScriptAsync("03-carga-exemplo.sql");
        await banco.AplicarScriptAsync("04-carga-volume.sql");
        await using var api = new ApiDeTestes(banco.ConnectionString);
        using var cliente = api.CreateClient();

        var pedidos = await LerAsync<PaginaDePedidos>(cliente, $"/pedidos?clienteId={clienteId}&dataInicio=2026-01-01&dataFim=2026-12-31");

        Assert.Equal(0, pedidos.TotalItens);
    }

    private static async Task<T> LerAsync<T>(HttpClient cliente, string caminho) =>
        await cliente.GetFromJsonAsync<T>(new Uri(caminho, UriKind.Relative), TestContext.Current.CancellationToken)
            ?? throw new InvalidOperationException("Resposta vazia.");

    private sealed record PaginaDePedidos(int TotalItens);

    private sealed record TopCliente(int ClienteId, string Nome, int QuantidadePedidos, decimal ValorTotal);
}
