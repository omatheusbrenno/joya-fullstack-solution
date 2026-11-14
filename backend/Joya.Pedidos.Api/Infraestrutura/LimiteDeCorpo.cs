using Microsoft.AspNetCore.Http.Features;

namespace Joya.Pedidos.Api.Infraestrutura;

/// <summary>
/// Corpo de requisição de até 64 KB (spec §2, CA17). Um Content-Length acima disso é recusado com 413 antes de
/// qualquer leitura, e o UseStatusCodePages o transforma em ProblemDetails. Corpo sem Content-Length (chunked)
/// fica limitado pelo servidor: o Kestrel interrompe a leitura ao passar do limite e responde 413.
/// </summary>
public static class LimiteDeCorpo
{
    public const long Bytes = 64 * 1024;

    public static IApplicationBuilder UseLimiteDeCorpo(this IApplicationBuilder app) =>
        app.Use((contexto, proximo) =>
        {
            if (contexto.Request.ContentLength > Bytes)
            {
                contexto.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
                return Task.CompletedTask;
            }

            if (contexto.Features.Get<IHttpMaxRequestBodySizeFeature>() is { IsReadOnly: false } limiteDoServidor)
            {
                limiteDoServidor.MaxRequestBodySize = Bytes;
            }

            return proximo(contexto);
        });
}
