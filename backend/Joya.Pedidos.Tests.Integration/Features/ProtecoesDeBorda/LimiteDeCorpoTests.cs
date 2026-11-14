using System.Net;
using System.Text;
using Joya.Pedidos.Tests.Integration.Infraestrutura;

namespace Joya.Pedidos.Tests.Integration.Features.ProtecoesDeBorda;

[Collection(ColecaoDoBanco.Nome)]
public sealed class LimiteDeCorpoTests(BancoDeTestes banco)
{
    private const int UmKb = 1024;

    [Fact]
    public async Task Corpo_acima_de_64_KB_e_recusado_com_413_em_ProblemDetails()
    {
        await using var api = new ApiDeTestes(banco.ConnectionString);
        using var cliente = api.CreateClient();

        using var resposta = await CriarPedidoAsync(cliente, CorpoComBytes((64 * UmKb) + 1));

        await ProblemaHttp.AssertAsync(resposta, HttpStatusCode.RequestEntityTooLarge);
    }

    [Fact]
    public async Task Corpo_de_exatamente_64_KB_chega_ao_endpoint()
    {
        await using var api = new ApiDeTestes(banco.ConnectionString);
        using var cliente = api.CreateClient();

        using var resposta = await CriarPedidoAsync(cliente, CorpoComBytes(64 * UmKb));

        // Sem Cliente nem Itens: o 400 prova que o corpo passou do limite e foi lido pelo endpoint.
        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    /// <summary>JSON válido com exatamente <paramref name="bytes"/> bytes, enchido por um campo que a API ignora.</summary>
    private static string CorpoComBytes(int bytes)
    {
        const string Moldura = """{"enchimento":""}""";
        return Moldura.Insert(Moldura.Length - 2, new string('a', bytes - Moldura.Length));
    }

    private static async Task<HttpResponseMessage> CriarPedidoAsync(HttpClient cliente, string corpo)
    {
        using var conteudo = new StringContent(corpo, Encoding.UTF8, "application/json");
        Assert.Equal(Encoding.UTF8.GetByteCount(corpo), conteudo.Headers.ContentLength);
        return await cliente.PostAsync(new Uri("/pedidos", UriKind.Relative), conteudo, TestContext.Current.CancellationToken);
    }
}
