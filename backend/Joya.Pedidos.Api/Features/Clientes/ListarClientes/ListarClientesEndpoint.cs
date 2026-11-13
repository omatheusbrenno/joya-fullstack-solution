namespace Joya.Pedidos.Api.Features.Clientes.ListarClientes;

public static class ListarClientesEndpoint
{
    public static IServiceCollection AddListarClientes(this IServiceCollection services) =>
        services.AddScoped<ListarClientesHandler>();

    /// <summary>GET /clientes: id e nome de todos os Clientes.</summary>
    public static IEndpointRouteBuilder MapListarClientes(this IEndpointRouteBuilder app)
    {
        app.MapGet("/clientes", async (ListarClientesHandler handler, CancellationToken cancellationToken) =>
            TypedResults.Ok(await handler.ExecutarAsync(cancellationToken)));
        return app;
    }
}
