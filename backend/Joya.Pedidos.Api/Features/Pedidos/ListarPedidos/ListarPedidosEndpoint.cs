namespace Joya.Pedidos.Api.Features.Pedidos.ListarPedidos;

public static class ListarPedidosEndpoint
{
    public static IServiceCollection AddListarPedidos(this IServiceCollection services) =>
        services.AddScoped<ListarPedidosHandler>();

    /// <summary>GET /pedidos: Pedidos do mais recente ao mais antigo, filtrados por Cliente e Período, paginados.</summary>
    public static IEndpointRouteBuilder MapListarPedidos(this IEndpointRouteBuilder app)
    {
        app.MapGet("/pedidos", async ([AsParameters] ListarPedidosRequest request, ListarPedidosHandler handler, CancellationToken cancellationToken) =>
            TypedResults.Ok(await handler.ExecutarAsync(request, cancellationToken)));
        return app;
    }
}
