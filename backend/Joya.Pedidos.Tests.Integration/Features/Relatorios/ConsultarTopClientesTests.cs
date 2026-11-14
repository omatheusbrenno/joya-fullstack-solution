using System.Net;
using System.Net.Http.Json;
using Joya.Pedidos.Tests.Integration.Infraestrutura;

namespace Joya.Pedidos.Tests.Integration.Features.Relatorios;

[Collection(ColecaoDoBanco.Nome)]
public sealed class ConsultarTopClientesTests(BancoDeTestes banco) : IAsyncLifetime
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

    [Fact]
    public async Task Clientes_homonimos_aparecem_como_entradas_distintas_com_valores_proprios()
    {
        var teclado = await _cenario.InserirProdutoAsync("Teclado", 150.00m);
        var ana = await _cenario.InserirClienteAsync("Ana Souza");
        var outraAna = await _cenario.InserirClienteAsync("Ana Souza");
        await _cenario.InserirPedidoAsync(ana, Utc(2026, 3, 10), (teclado, 1, 150.00m));
        await _cenario.InserirPedidoAsync(outraAna, Utc(2026, 3, 11), (teclado, 2, 149.99m));
        await _cenario.InserirPedidoAsync(outraAna, Utc(2026, 3, 12), (teclado, 1, 150.00m));

        var top = await ConsultarAsync("/relatorios/top-clientes?dataInicio=2026-03-01&dataFim=2026-03-31");

        Assert.Equal(
            [
                new TopCliente(outraAna, "Ana Souza", 2, 449.98m),
                new TopCliente(ana, "Ana Souza", 1, 150.00m),
            ],
            top);
    }

    [Fact]
    public async Task Considera_so_os_Pedidos_do_Periodo_com_as_duas_pontas_inclusivas_e_desempata_pelo_id()
    {
        var teclado = await _cenario.InserirProdutoAsync("Teclado", 150.00m);
        var primeiro = await _cenario.InserirClienteAsync("Bruno Lima");
        var segundo = await _cenario.InserirClienteAsync("Carla Mendes");
        var semPedidosNoPeriodo = await _cenario.InserirClienteAsync("Diego Ferreira");
        await _cenario.InserirClienteAsync("Elisa Rocha");
        await _cenario.InserirPedidoAsync(semPedidosNoPeriodo, Utc(2026, 2, 28, 23, 59, 59), (teclado, 9, 150.00m));
        await _cenario.InserirPedidoAsync(semPedidosNoPeriodo, Utc(2026, 4, 1, 0, 0, 0), (teclado, 9, 150.00m));
        await _cenario.InserirPedidoAsync(segundo, Utc(2026, 3, 1, 0, 0, 0), (teclado, 1, 150.00m));
        await _cenario.InserirPedidoAsync(primeiro, Utc(2026, 3, 31, 23, 59, 59), (teclado, 1, 150.00m));

        var top = await ConsultarAsync("/relatorios/top-clientes?dataInicio=2026-03-01&dataFim=2026-03-31");

        Assert.Equal(
            [
                new TopCliente(primeiro, "Bruno Lima", 1, 150.00m),
                new TopCliente(segundo, "Carla Mendes", 1, 150.00m),
            ],
            top);
    }

    [Fact]
    public async Task Periodo_sem_Pedidos_devolve_lista_vazia()
    {
        var teclado = await _cenario.InserirProdutoAsync("Teclado", 150.00m);
        var ana = await _cenario.InserirClienteAsync("Ana Souza");
        await _cenario.InserirPedidoAsync(ana, Utc(2026, 3, 10), (teclado, 1, 150.00m));

        Assert.Empty(await ConsultarAsync("/relatorios/top-clientes?dataInicio=2025-01-01&dataFim=2025-12-31"));
    }

    [Fact]
    public async Task Periodo_ate_a_ultima_data_possivel_inclui_os_Pedidos_ate_hoje()
    {
        var teclado = await _cenario.InserirProdutoAsync("Teclado", 150.00m);
        var ana = await _cenario.InserirClienteAsync("Ana Souza");
        await _cenario.InserirPedidoAsync(ana, Utc(2026, 3, 10), (teclado, 1, 150.00m));

        var top = await ConsultarAsync("/relatorios/top-clientes?dataInicio=2026-01-01&dataFim=9999-12-31");

        Assert.Equal([new TopCliente(ana, "Ana Souza", 1, 150.00m)], top);
    }

    [Theory]
    [InlineData("/relatorios/top-clientes", new[] { "dataInicio", "dataFim" })]
    [InlineData("/relatorios/top-clientes?dataFim=2026-03-31", new[] { "dataInicio" })]
    [InlineData("/relatorios/top-clientes?dataInicio=2026-03-01", new[] { "dataFim" })]
    [InlineData("/relatorios/top-clientes?dataInicio=2026-03-02&dataFim=2026-03-01", new[] { "dataFim" })]
    public async Task Periodo_ausente_ou_invertido_devolve_400_em_ProblemDetails_apontando_os_campos(string caminho, string[] campos)
    {
        var resposta = await _cliente.GetAsync(new Uri(caminho, UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Equal("application/problem+json", resposta.Content.Headers.ContentType?.MediaType);
        var problema = await resposta.Content.ReadFromJsonAsync<ProblemaDeValidacao>(TestContext.Current.CancellationToken);
        Assert.NotNull(problema);
        Assert.Equal(400, problema.Status);
        Assert.Equal(campos.Order(StringComparer.Ordinal), problema.Errors.Keys.Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task Data_em_formato_invalido_devolve_400_em_ProblemDetails()
    {
        var resposta = await _cliente.GetAsync(
            new Uri("/relatorios/top-clientes?dataInicio=01/03/2026&dataFim=2026-03-31", UriKind.Relative),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Equal("application/problem+json", resposta.Content.Headers.ContentType?.MediaType);
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

    private sealed record ProblemaDeValidacao(int Status, Dictionary<string, string[]> Errors);

    private sealed record TopCliente(int ClienteId, string Nome, int QuantidadePedidos, decimal ValorTotal);
}
