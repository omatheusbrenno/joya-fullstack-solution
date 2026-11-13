namespace Joya.Pedidos.Api.Features.Pedidos.ListarPedidos;

public static class ListarPedidosEndpoint
{
    public static IServiceCollection AddListarPedidos(this IServiceCollection services) =>
        services.AddScoped<ListarPedidosHandler>();

    /// <summary>GET /pedidos: Pedidos do mais recente ao mais antigo, paginados.</summary>
    public static IEndpointRouteBuilder MapListarPedidos(this IEndpointRouteBuilder app)
    {
        app.MapGet("/pedidos", async (ListarPedidosHandler handler, CancellationToken cancellationToken, int pagina = 1, int tamanhoPagina = 20) =>
            TypedResults.Ok(await handler.ExecutarAsync(pagina, tamanhoPagina, cancellationToken)));
        return app;
    }
}
