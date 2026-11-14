namespace Joya.Pedidos.Api.Infraestrutura;

/// <summary>
/// Headers de segurança em toda resposta da API (OWASP A02). A CSP restritiva de verdade fica no nginx, que serve
/// o HTML do SPA. Aqui, como a API só devolve JSON, a CSP é a mínima, como defesa em profundidade.
/// Os headers entram no OnStarting porque o manipulador de exceções limpa os headers antes de escrever o 500.
/// </summary>
public static class HeadersDeSeguranca
{
    public static IApplicationBuilder UseHeadersDeSeguranca(this IApplicationBuilder app) =>
        app.Use((contexto, proximo) =>
        {
            contexto.Response.OnStarting(() =>
            {
                var headers = contexto.Response.Headers;
                headers.XContentTypeOptions = "nosniff";
                headers["Referrer-Policy"] = "no-referrer";
                headers.ContentSecurityPolicy = "default-src 'none'; frame-ancestors 'none'";
                return Task.CompletedTask;
            });
            return proximo(contexto);
        });
}
