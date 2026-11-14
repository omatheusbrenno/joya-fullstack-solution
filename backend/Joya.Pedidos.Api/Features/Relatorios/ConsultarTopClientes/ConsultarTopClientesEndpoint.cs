namespace Joya.Pedidos.Api.Features.Relatorios.ConsultarTopClientes;

public static class ConsultarTopClientesEndpoint
{
    public static IServiceCollection AddConsultarTopClientes(this IServiceCollection services) =>
        services.AddScoped<ConsultarTopClientesHandler>();

    /// <summary>GET /relatorios/top-clientes: os até cinco Clientes com maior Valor Total no Período.</summary>
    public static IEndpointRouteBuilder MapConsultarTopClientes(this IEndpointRouteBuilder app)
    {
        app.MapGet("/relatorios/top-clientes", async ([AsParameters] ConsultarTopClientesRequest request, ConsultarTopClientesHandler handler, CancellationToken cancellationToken) =>
            TypedResults.Ok(await handler.ExecutarAsync(request, cancellationToken)));
        return app;
    }
}
