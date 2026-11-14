namespace Joya.Pedidos.Api.Features.Saude;

public static class SaudeEndpoint
{
    public static IServiceCollection AddSaude(this IServiceCollection services)
    {
        services.AddHealthChecks().AddCheck<VerificacaoDoBanco>("banco");
        return services;
    }

    /// <summary>
    /// GET /health: "Healthy" (200) ou "Unhealthy" (503), sem detalhes internos. Fica fora do rate limit: o health
    /// check da plataforma pode chegar pelo mesmo IP de proxy que os visitantes, e um 429 aqui derrubaria a API.
    /// O custo de deixá-lo livre é baixo: é um SELECT 1 sobre o pool de conexões.
    /// </summary>
    public static IEndpointRouteBuilder MapSaude(this IEndpointRouteBuilder app)
    {
        app.MapHealthChecks("/health").DisableRateLimiting();
        return app;
    }
}
