using System.Net;
using Joya.Pedidos.Tests.Integration.Infraestrutura;

namespace Joya.Pedidos.Tests.Integration.Features.ProtecoesDeBorda;

[Collection(ColecaoDoBanco.Nome)]
public sealed class LimiteDeRequisicoesTests(BancoDeTestes banco) : IAsyncLifetime
{
    private static readonly Uri Produtos = new("/produtos", UriKind.Relative);

    private ApiDeTestes _api = null!;
    private HttpClient _cliente = null!;

    public ValueTask InitializeAsync()
    {
        _api = new ApiDeTestes(banco.ConnectionString);
        _cliente = _api.CreateClient();
        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        _cliente.Dispose();
        await _api.DisposeAsync();
    }

    [Fact]
    public async Task Acima_de_100_requisicoes_por_minuto_do_mesmo_IP_devolve_429_em_ProblemDetails()
    {
        for (var i = 0; i < 100; i++)
        {
            using var permitida = await GetAsync(Produtos);
            Assert.Equal(HttpStatusCode.OK, permitida.StatusCode);
        }

        using var resposta = await GetAsync(Produtos);

        await ProblemaHttp.AssertAsync(resposta, HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Acima_de_20_POST_pedidos_por_minuto_do_mesmo_IP_devolve_429_e_as_outras_rotas_seguem_liberadas()
    {
        for (var i = 0; i < 20; i++)
        {
            using var permitida = await CriarPedidoAsync();
            Assert.Equal(HttpStatusCode.BadRequest, permitida.StatusCode);
        }

        using var resposta = await CriarPedidoAsync();

        await ProblemaHttp.AssertAsync(resposta, HttpStatusCode.TooManyRequests);
        using var leitura = await GetAsync(Produtos);
        Assert.Equal(HttpStatusCode.OK, leitura.StatusCode);
    }

    [Fact]
    public async Task Health_fica_fora_do_limite_para_a_plataforma_nao_derrubar_a_API_por_429()
    {
        for (var i = 0; i < 100; i++)
        {
            using var permitida = await GetAsync(Produtos);
        }

        using var esgotado = await GetAsync(Produtos);
        using var saude = await GetAsync(new Uri("/health", UriKind.Relative));

        Assert.Equal(HttpStatusCode.TooManyRequests, esgotado.StatusCode);
        Assert.Equal(HttpStatusCode.OK, saude.StatusCode);
    }

    private async Task<HttpResponseMessage> GetAsync(Uri uri) => await _cliente.GetAsync(uri, TestContext.Current.CancellationToken);

    // Corpo sem Cliente nem Itens: 400 rápido, sem tocar o banco. O limite conta a requisição mesmo assim.
    private async Task<HttpResponseMessage> CriarPedidoAsync() => await _cliente.PostComoClienteAsync("/pedidos", new { }, ipDoCliente: null);
}
