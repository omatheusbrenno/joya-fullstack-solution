namespace Joya.Pedidos.Api.Features.Saude;

public static class SaudeEndpoint
{
    public static IServiceCollection AddSaude(this IServiceCollection services)
    {
        services.AddHealthChecks().AddCheck<VerificacaoDoBanco>("banco");
        return services;
    }

    /// <summary>GET /health: "Healthy" (200) ou "Unhealthy" (503), sem detalhes internos.</summary>
    public static IEndpointRouteBuilder MapSaude(this IEndpointRouteBuilder app)
    {
        app.MapHealthChecks("/health");
        return app;
    }
}
