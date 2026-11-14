using System.Net;
using System.Net.Http.Json;
using Joya.Pedidos.Tests.Integration.Infraestrutura;

namespace Joya.Pedidos.Tests.Integration.Features.Relatorios;

[Collection(ColecaoDoBanco.Nome)]
public sealed class TopClientesTests(BancoDeTestes banco) : IAsyncLifetime
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
    public async Task Devolve_no_maximo_5_Clientes_em_ordem_decrescente_de_Valor_Total_com_a_quantidade_de_Pedidos()
    {
        var teclado = await _cenario.InserirProdutoAsync("Teclado", 150.00m);
        var mouse = await _cenario.InserirProdutoAsync("Mouse", 79.90m);
        var clientes = new Dictionary<string, int>();
        foreach (var nome in new[] { "Ana", "Bruno", "Carla", "Diego", "Elisa", "Fernanda" })
        {
            clientes[nome] = await _cenario.InserirClienteAsync(nome);
        }

        // Fernanda tem o menor Valor Total e fica de fora do Top 5.
        await _cenario.InserirPedidoAsync(clientes["Fernanda"], Utc(2026, 3, 1), (mouse, 1, 79.90m));
        await _cenario.InserirPedidoAsync(clientes["Elisa"], Utc(2026, 3, 2), (teclado, 1, 150.00m));
        await _cenario.InserirPedidoAsync(clientes["Diego"], Utc(2026, 3, 3), (teclado, 1, 150.00m), (mouse, 1, 79.90m));
        await _cenario.InserirPedidoAsync(clientes["Carla"], Utc(2026, 3, 4), (teclado, 2, 150.00m));
        await _cenario.InserirPedidoAsync(clientes["Carla"], Utc(2026, 3, 5), (mouse, 1, 75.50m));
        await _cenario.InserirPedidoAsync(clientes["Bruno"], Utc(2026, 3, 6), (teclado, 3, 140.00m));
        await _cenario.InserirPedidoAsync(clientes["Ana"], Utc(2026, 3, 7), (teclado, 5, 150.00m), (mouse, 2, 79.90m));
        await _cenario.InserirPedidoAsync(clientes["Ana"], Utc(2026, 3, 8), (mouse, 1, 79.90m));
        await _cenario.InserirPedidoAsync(clientes["Ana"], Utc(2026, 3, 9), (teclado, 1, 150.00m));

        var top = await ConsultarAsync("/relatorios/top-clientes?dataInicio=2026-03-01&dataFim=2026-03-31");

        Assert.Equal(
            [
                new TopCliente(clientes["Ana"], "Ana", 3, 1139.70m),
                new TopCliente(clientes["Bruno"], "Bruno", 1, 420.00m),
                new TopCliente(clientes["Carla"], "Carla", 2, 375.50m),
                new TopCliente(clientes["Diego"], "Diego", 1, 229.90m),
                new TopCliente(clientes["Elisa"], "Elisa", 1, 150.00m),
            ],
            top);
    }

    private async Task<TopCliente[]> ConsultarAsync(string caminho)
    {
        var resposta = await _cliente.GetAsync(new Uri(caminho, UriKind.Relative), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        return await resposta.Content.ReadFromJsonAsync<TopCliente[]>(TestContext.Current.CancellationToken)
            ?? throw new InvalidOperationException("Resposta vazia.");
    }

    private static DateTime Utc(int ano, int mes, int dia, int hora = 12, int minuto = 0, int segundo = 0) =>
        new(ano, mes, dia, hora, minuto, segundo, DateTimeKind.Utc);

    private sealed record TopCliente(int ClienteId, string Nome, int QuantidadePedidos, decimal ValorTotal);
}
