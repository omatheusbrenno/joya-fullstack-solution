using System.Net;
using Joya.Pedidos.Tests.Integration.Infraestrutura;

namespace Joya.Pedidos.Tests.Integration.Features.ProtecoesDeBorda;

[Collection(ColecaoDoBanco.Nome)]
public sealed class HeadersDeSegurancaTests(BancoDeTestes banco)
{
    private static readonly Uri Produtos = new("/produtos", UriKind.Relative);

    [Fact]
    public async Task Resposta_de_sucesso_traz_os_headers_de_seguranca()
    {
        await using var api = new ApiDeTestes(banco.ConnectionString);
        using var cliente = api.CreateClient();

        using var resposta = await cliente.GetAsync(Produtos, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        AssertHeadersDeSeguranca(resposta);
    }

    [Fact]
    public async Task Resposta_de_erro_inesperado_tambem_traz_os_headers_de_seguranca()
    {
        await using var api = new ApiDeTestes("Server=127.0.0.1,1;Database=JoyaPedidos;User Id=sa;Password=x;Connect Timeout=1;TrustServerCertificate=True");
        using var cliente = api.CreateClient();

        using var resposta = await cliente.GetAsync(Produtos, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.InternalServerError, resposta.StatusCode);
        AssertHeadersDeSeguranca(resposta);
    }

    [Theory]
    [InlineData("https", true)]
    [InlineData("http", false)]
    public async Task HSTS_so_sai_quando_o_proxy_confiavel_informa_que_a_conexao_original_era_HTTPS(string protocolo, bool esperaHsts)
    {
        await using var api = new ApiDeTestes(banco.ConnectionString);
        using var cliente = api.CreateClient();
        using var requisicao = new HttpRequestMessage(HttpMethod.Get, Produtos);
        requisicao.Headers.Host = "pedidos.exemplo.com"; // o HSTS do ASP.NET nunca sai para localhost
        requisicao.Headers.Add("X-Forwarded-For", "198.51.100.1");
        requisicao.Headers.Add("X-Forwarded-Proto", protocolo);

        using var resposta = await cliente.SendAsync(requisicao, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal(esperaHsts, resposta.Headers.Contains("Strict-Transport-Security"));
    }

    private static void AssertHeadersDeSeguranca(HttpResponseMessage resposta)
    {
        Assert.Equal(["nosniff"], resposta.Headers.GetValues("X-Content-Type-Options"));
        Assert.Equal(["no-referrer"], resposta.Headers.GetValues("Referrer-Policy"));
        Assert.Equal(["default-src 'none'; frame-ancestors 'none'"], resposta.Headers.GetValues("Content-Security-Policy"));
    }
}
