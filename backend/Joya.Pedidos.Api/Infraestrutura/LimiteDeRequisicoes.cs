using System.Threading.RateLimiting;

namespace Joya.Pedidos.Api.Infraestrutura;

/// <summary>
/// Rate limiting por IP do cliente (spec §2, CA17): 100 requisições por minuto em qualquer rota e, além disso,
/// 20 por minuto em POST /pedidos (política <see cref="CriacaoDePedido"/>). Acima disso, 429
/// sem corpo, que o UseStatusCodePages transforma em ProblemDetails (ADR 0008). O IP é o que sobra depois dos
/// forwarded headers, ou seja, o do cliente real atrás de um proxy confiável (ADR 0011).
/// </summary>
public static class LimiteDeRequisicoes
{
    public const string CriacaoDePedido = "criacao-de-pedido";

    private const int PorMinutoNoGeral = 100;
    private const int PorMinutoNaCriacaoDePedido = 20;

    public static IServiceCollection AddLimiteDeRequisicoes(this IServiceCollection services) =>
        services.AddRateLimiter(opcoes =>
        {
            opcoes.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            opcoes.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(contexto => PorIp(contexto, PorMinutoNoGeral));
            opcoes.AddPolicy(CriacaoDePedido, contexto => PorIp(contexto, PorMinutoNaCriacaoDePedido));
        });

    private static RateLimitPartition<string> PorIp(HttpContext contexto, int limitePorMinuto) =>
        RateLimitPartition.GetFixedWindowLimiter(
            contexto.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = limitePorMinuto, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 });
}
