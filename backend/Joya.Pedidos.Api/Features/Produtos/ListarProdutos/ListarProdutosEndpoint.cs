namespace Joya.Pedidos.Api.Features.Produtos.ListarProdutos;

public static class ListarProdutosEndpoint
{
    public static IServiceCollection AddListarProdutos(this IServiceCollection services) =>
        services.AddScoped<ListarProdutosHandler>();

    /// <summary>GET /produtos: id, nome, preço e Estoque de todos os Produtos.</summary>
    public static IEndpointRouteBuilder MapListarProdutos(this IEndpointRouteBuilder app)
    {
        app.MapGet("/produtos", async (ListarProdutosHandler handler, CancellationToken cancellationToken) =>
            TypedResults.Ok(await handler.ExecutarAsync(cancellationToken)));
        return app;
    }
}
