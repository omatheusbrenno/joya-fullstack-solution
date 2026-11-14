using System.Net.Http.Json;

namespace Joya.Pedidos.Tests.Integration.Infraestrutura;

public static class RequisicoesDeCliente
{
    /// <summary>
    /// POST de JSON como se viesse do cliente <paramref name="ipDoCliente"/> atrás do proxy confiável do
    /// <see cref="ApiDeTestes"/>, que o informa em X-Forwarded-For. Sem IP, vale o da conexão.
    /// </summary>
    public static async Task<HttpResponseMessage> PostComoClienteAsync(this HttpClient cliente, string caminho, object corpo, string? ipDoCliente)
    {
        using var requisicao = new HttpRequestMessage(HttpMethod.Post, new Uri(caminho, UriKind.Relative)) { Content = JsonContent.Create(corpo) };
        if (ipDoCliente is not null)
        {
            requisicao.Headers.Add("X-Forwarded-For", ipDoCliente);
        }

        return await cliente.SendAsync(requisicao, TestContext.Current.CancellationToken);
    }
}
