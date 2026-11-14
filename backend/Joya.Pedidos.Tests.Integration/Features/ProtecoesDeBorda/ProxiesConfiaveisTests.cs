using System.Net;
using System.Net.Http.Json;
using Joya.Pedidos.Tests.Integration.Infraestrutura;

namespace Joya.Pedidos.Tests.Integration.Features.ProtecoesDeBorda;

/// <summary>O rate limit vê o IP real do cliente só quando a requisição chega por um proxy confiável (ADR 0011).</summary>
[Collection(ColecaoDoBanco.Nome)]
public sealed class ProxiesConfiaveisTests(BancoDeTestes banco)
{
    [Fact]
    public async Task Atras_do_proxy_confiavel_cada_IP_do_X_Forwarded_For_tem_o_proprio_limite()
    {
        await using var api = new ApiDeTestes(banco.ConnectionString);
        using var cliente = api.CreateClient();

        for (var i = 0; i < 20; i++)
        {
            using var permitida = await CriarPedidoAsync(cliente, "198.51.100.1");
        }

        using var esgotado = await CriarPedidoAsync(cliente, "198.51.100.1");
        using var outroCliente = await CriarPedidoAsync(cliente, "198.51.100.2");

        Assert.Equal(HttpStatusCode.TooManyRequests, esgotado.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, outroCliente.StatusCode);
    }

    [Fact]
    public async Task De_origem_nao_confiavel_o_X_Forwarded_For_e_ignorado_e_o_limite_vale_para_o_IP_da_conexao()
    {
        await using var api = new ApiDeTestes(banco.ConnectionString, ipRemoto: IPAddress.Parse("203.0.113.7"));
        using var cliente = api.CreateClient();

        // Um IP forjado diferente a cada requisição não dribla o limite.
        for (var i = 0; i < 20; i++)
        {
            using var permitida = await CriarPedidoAsync(cliente, $"198.51.100.{i + 1}");
        }

        using var resposta = await CriarPedidoAsync(cliente, "198.51.100.200");

        Assert.Equal(HttpStatusCode.TooManyRequests, resposta.StatusCode);
    }

    private static async Task<HttpResponseMessage> CriarPedidoAsync(HttpClient cliente, string ipDoCliente)
    {
        using var requisicao = new HttpRequestMessage(HttpMethod.Post, new Uri("/pedidos", UriKind.Relative))
        {
            Content = JsonContent.Create(new { }),
        };
        requisicao.Headers.Add("X-Forwarded-For", ipDoCliente);
        return await cliente.SendAsync(requisicao, TestContext.Current.CancellationToken);
    }
}
