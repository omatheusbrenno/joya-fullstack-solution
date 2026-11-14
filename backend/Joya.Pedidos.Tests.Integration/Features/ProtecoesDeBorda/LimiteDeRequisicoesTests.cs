using System.Net;
using System.Net.Http.Json;
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

        await AssertProblemaAsync(resposta, HttpStatusCode.TooManyRequests);
    }

    private async Task<HttpResponseMessage> GetAsync(Uri uri) => await _cliente.GetAsync(uri, TestContext.Current.CancellationToken);

    internal static async Task AssertProblemaAsync(HttpResponseMessage resposta, HttpStatusCode statusEsperado)
    {
        Assert.Equal(statusEsperado, resposta.StatusCode);
        Assert.Equal("application/problem+json", resposta.Content.Headers.ContentType?.MediaType);
        var problema = await resposta.Content.ReadFromJsonAsync<Problema>(TestContext.Current.CancellationToken);
        Assert.Equal((int)statusEsperado, problema?.Status);
    }

    private sealed record Problema(int Status);
}
