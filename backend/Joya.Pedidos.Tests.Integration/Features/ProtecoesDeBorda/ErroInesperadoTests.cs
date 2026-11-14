using System.Net;
using Joya.Pedidos.Tests.Integration.Infraestrutura;

namespace Joya.Pedidos.Tests.Integration.Features.ProtecoesDeBorda;

public sealed class ErroInesperadoTests
{
    [Fact]
    public async Task Excecao_inesperada_devolve_500_em_ProblemDetails_sem_stack_trace_nem_detalhes_internos()
    {
        await using var api = new ApiDeTestes(ApiDeTestes.BancoInalcancavel);
        using var cliente = api.CreateClient();

        using var resposta = await cliente.GetAsync(new Uri("/produtos", UriKind.Relative), TestContext.Current.CancellationToken);

        await ProblemaHttp.AssertAsync(resposta, HttpStatusCode.InternalServerError);
        var corpo = await resposta.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.DoesNotContain("Sql", corpo, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SenhaSecreta", corpo, StringComparison.Ordinal);
        Assert.DoesNotContain("127.0.0.1", corpo, StringComparison.Ordinal);
        Assert.DoesNotContain(" at ", corpo, StringComparison.Ordinal);
        Assert.DoesNotContain("exception", corpo, StringComparison.OrdinalIgnoreCase);
    }
}
