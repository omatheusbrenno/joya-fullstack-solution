using System.Net;
using System.Net.Http.Json;
using Joya.Pedidos.Tests.Integration.Infraestrutura;

namespace Joya.Pedidos.Tests.Integration.Features.Pedidos;

[Collection(ColecaoDoBanco.Nome)]
public sealed class ListarPedidosTests(BancoDeTestes banco) : IAsyncLifetime
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
    public async Task Sem_filtros_lista_do_mais_recente_ao_mais_antigo_com_cliente_itens_e_valor_total()
    {
        var ana = await _cenario.InserirClienteAsync("Ana Souza");
        var bruno = await _cenario.InserirClienteAsync("Bruno Lima");
        var teclado = await _cenario.InserirProdutoAsync("Teclado", 150.00m);
        var mouse = await _cenario.InserirProdutoAsync("Mouse", 80.00m);
        var antigo = await _cenario.InserirPedidoAsync(ana, Utc(2026, 1, 10, 9, 0, 0), (teclado, 2, 150.00m));
        var recente = await _cenario.InserirPedidoAsync(bruno, Utc(2026, 3, 5, 14, 30, 0), (teclado, 1, 140.00m), (mouse, 3, 79.90m));

        var pagina = await ListarAsync("/pedidos");

        Assert.Equal(1, pagina.Pagina);
        Assert.Equal(20, pagina.TamanhoPagina);
        Assert.Equal(2, pagina.TotalItens);
        Assert.Equal(
            [
                new PedidoListado(recente, bruno, "Bruno Lima", Utc(2026, 3, 5, 14, 30, 0), 2, 379.70m),
                new PedidoListado(antigo, ana, "Ana Souza", Utc(2026, 1, 10, 9, 0, 0), 1, 300.00m),
            ],
            pagina.Itens);
    }

    [Fact]
    public async Task Pagina_com_20_por_padrao_desempata_pelo_id_e_conta_o_total_de_Pedidos()
    {
        var ana = await _cenario.InserirClienteAsync("Ana Souza");
        var teclado = await _cenario.InserirProdutoAsync("Teclado", 150.00m);
        var mesmaData = Utc(2026, 2, 1, 12, 0, 0);
        var ids = new List<int>();
        for (var i = 0; i < 25; i++)
        {
            ids.Add(await _cenario.InserirPedidoAsync(ana, mesmaData, (teclado, 1, 150.00m)));
        }

        var primeira = await ListarAsync("/pedidos");
        var segunda = await ListarAsync("/pedidos?pagina=2");
        var paginaDe10 = await ListarAsync("/pedidos?pagina=3&tamanhoPagina=10");

        var idsDecrescentes = ids.OrderDescending().ToArray();
        Assert.Equal(idsDecrescentes[..20], primeira.Itens.Select(p => p.Id));
        Assert.Equal(idsDecrescentes[20..], segunda.Itens.Select(p => p.Id));
        Assert.Equal(idsDecrescentes[20..], paginaDe10.Itens.Select(p => p.Id));
        Assert.All([primeira, segunda, paginaDe10], p => Assert.Equal(25, p.TotalItens));
        Assert.Equal((2, 20), (segunda.Pagina, segunda.TamanhoPagina));
        Assert.Equal((3, 10), (paginaDe10.Pagina, paginaDe10.TamanhoPagina));
    }

    [Fact]
    public async Task Filtra_por_Cliente_e_por_Periodo_com_as_duas_pontas_inclusivas_e_combinados()
    {
        var ana = await _cenario.InserirClienteAsync("Ana Souza");
        var outraAna = await _cenario.InserirClienteAsync("Ana Souza");
        var teclado = await _cenario.InserirProdutoAsync("Teclado", 150.00m);
        var antesDoPeriodo = await _cenario.InserirPedidoAsync(ana, Utc(2026, 2, 28, 23, 59, 59), (teclado, 1, 150.00m));
        var inicioDoPeriodo = await _cenario.InserirPedidoAsync(ana, Utc(2026, 3, 1, 0, 0, 0), (teclado, 1, 150.00m));
        var fimDoPeriodo = await _cenario.InserirPedidoAsync(ana, Utc(2026, 3, 31, 23, 59, 59), (teclado, 1, 150.00m));
        var depoisDoPeriodo = await _cenario.InserirPedidoAsync(ana, Utc(2026, 4, 1, 0, 0, 0), (teclado, 1, 150.00m));
        var daOutraAna = await _cenario.InserirPedidoAsync(outraAna, Utc(2026, 3, 15, 10, 0, 0), (teclado, 1, 150.00m));

        Assert.Equal([depoisDoPeriodo, fimDoPeriodo, inicioDoPeriodo, antesDoPeriodo], await IdsAsync($"/pedidos?clienteId={ana}"));
        Assert.Equal([fimDoPeriodo, daOutraAna, inicioDoPeriodo], await IdsAsync("/pedidos?dataInicio=2026-03-01&dataFim=2026-03-31"));
        Assert.Equal([depoisDoPeriodo, fimDoPeriodo, daOutraAna, inicioDoPeriodo], await IdsAsync("/pedidos?dataInicio=2026-03-01"));
        Assert.Equal([fimDoPeriodo, daOutraAna, inicioDoPeriodo, antesDoPeriodo], await IdsAsync("/pedidos?dataFim=2026-03-31"));
        Assert.Equal([fimDoPeriodo, inicioDoPeriodo], await IdsAsync($"/pedidos?clienteId={ana}&dataInicio=2026-03-01&dataFim=2026-03-31"));
        Assert.Equal([fimDoPeriodo], await IdsAsync("/pedidos?dataInicio=2026-03-31&dataFim=2026-03-31&clienteId=" + ana));

        var filtrada = await ListarAsync($"/pedidos?clienteId={outraAna}");
        Assert.Equal(1, filtrada.TotalItens);
    }

    private async Task<int[]> IdsAsync(string caminho) => [.. (await ListarAsync(caminho)).Itens.Select(p => p.Id)];

    private async Task<PaginaDePedidos> ListarAsync(string caminho)
    {
        var resposta = await _cliente.GetAsync(new Uri(caminho, UriKind.Relative), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        return await resposta.Content.ReadFromJsonAsync<PaginaDePedidos>(TestContext.Current.CancellationToken)
            ?? throw new InvalidOperationException("Resposta vazia.");
    }

    private static DateTime Utc(int ano, int mes, int dia, int hora, int minuto, int segundo) =>
        new(ano, mes, dia, hora, minuto, segundo, DateTimeKind.Utc);

    private sealed record PedidoListado(int Id, int ClienteId, string NomeCliente, DateTime DataPedido, int QuantidadeItens, decimal ValorTotal);

    private sealed record PaginaDePedidos(PedidoListado[] Itens, int Pagina, int TamanhoPagina, int TotalItens);
}
