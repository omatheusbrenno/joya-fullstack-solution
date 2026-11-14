namespace Joya.Pedidos.Api.Features.Relatorios.TopClientes;

public static class TopClientesEndpoint
{
    public static IServiceCollection AddTopClientes(this IServiceCollection services) =>
        services.AddScoped<TopClientesHandler>();

    /// <summary>GET /relatorios/top-clientes: os até cinco Clientes com maior Valor Total no Período.</summary>
    public static IEndpointRouteBuilder MapTopClientes(this IEndpointRouteBuilder app)
    {
        app.MapGet("/relatorios/top-clientes", async ([AsParameters] TopClientesRequest request, TopClientesHandler handler, CancellationToken cancellationToken) =>
            TypedResults.Ok(await handler.ExecutarAsync(request, cancellationToken)));
        return app;
    }
}
