using System.Net;
using System.Net.Http.Json;

namespace Joya.Pedidos.Tests.Integration.Infraestrutura;

/// <summary>Confere uma resposta de erro sem campos por Item: status, application/problem+json e o status no corpo (ADR 0008).</summary>
public static class ProblemaHttp
{
    public static async Task AssertAsync(HttpResponseMessage resposta, HttpStatusCode statusEsperado)
    {
        Assert.Equal(statusEsperado, resposta.StatusCode);
        Assert.Equal("application/problem+json", resposta.Content.Headers.ContentType?.MediaType);
        var problema = await resposta.Content.ReadFromJsonAsync<Problema>(TestContext.Current.CancellationToken);
        Assert.Equal((int)statusEsperado, problema?.Status);
    }

    private sealed record Problema(int Status);
}
