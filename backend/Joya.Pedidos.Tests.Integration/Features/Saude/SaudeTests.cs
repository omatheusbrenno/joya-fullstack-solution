using System.Net;
using Joya.Pedidos.Tests.Integration.Infraestrutura;

namespace Joya.Pedidos.Tests.Integration.Features.Saude;

[Collection(ColecaoDoBanco.Nome)]
public sealed class SaudeTests(BancoDeTestes banco)
{
    [Fact]
    public async Task Health_com_banco_no_ar_responde_saudavel()
    {
        await using var api = new ApiDeTestes(banco.ConnectionString);
        using var cliente = api.CreateClient();

        var resposta = await cliente.GetAsync(new Uri("/health", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal("Healthy", await resposta.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Health_sem_banco_responde_indisponivel_sem_detalhes_internos()
    {
        await using var api = new ApiDeTestes(ApiDeTestes.BancoInalcancavel);
        using var cliente = api.CreateClient();

        var resposta = await cliente.GetAsync(new Uri("/health", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, resposta.StatusCode);
        Assert.Equal("Unhealthy", await resposta.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }
}
