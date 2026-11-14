using Joya.Pedidos.Api.Infraestrutura;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Joya.Pedidos.Api.Features.Pedidos.CriarPedido;

public static class CriarPedidoEndpoint
{
    public static IServiceCollection AddCriarPedido(this IServiceCollection services) =>
        services.AddScoped<CriarPedidoHandler>();

    /// <summary>POST /pedidos: cria o Pedido com Baixa de Estoque atômica. 201, 400 (forma), 422 (negócio) ou 429 (20/min por IP).</summary>
    public static IEndpointRouteBuilder MapCriarPedido(this IEndpointRouteBuilder app)
    {
        app.MapPost("/pedidos", async (CriarPedidoRequest request, CriarPedidoHandler handler, CancellationToken cancellationToken) =>
            ParaHttp(await handler.ExecutarAsync(request, cancellationToken)))
            .RequireRateLimiting(LimiteDeRequisicoes.CriacaoDePedido);
        return app;
    }

    // Sem Location: não existe GET /pedidos/{id} (fora de escopo na spec). O Pedido criado vai no corpo.
    internal static Results<Created<CriarPedidoResponse>, ValidationProblem, ProblemHttpResult> ParaHttp(ResultadoDeCriarPedido resultado) =>
        resultado switch
        {
            PedidoCriado criado => TypedResults.Created((string?)null, criado.Pedido),
            PedidoNaoCriado { Violacoes: var violacoes } when violacoes.Any(v => v.Tipo == TipoDeViolacao.Forma) =>
                TypedResults.ValidationProblem(PorCampo(violacoes)),
            PedidoNaoCriado { Violacoes: var violacoes } => TypedResults.Problem(new HttpValidationProblemDetails(PorCampo(violacoes))
            {
                Status = StatusCodes.Status422UnprocessableEntity,
                Title = "O Pedido viola regras de negócio.",
            }),
            _ => throw new InvalidOperationException($"Resultado desconhecido: {resultado.GetType().Name}."),
        };

    /// <summary>Mesmo formato do 400 (ADR 0008): erros agrupados por campo, todos de uma vez (RN11).</summary>
    private static Dictionary<string, string[]> PorCampo(IReadOnlyList<Violacao> violacoes) =>
        violacoes
            .GroupBy(violacao => violacao.Campo, StringComparer.Ordinal)
            .ToDictionary(grupo => grupo.Key, grupo => grupo.Select(violacao => violacao.Mensagem).ToArray(), StringComparer.Ordinal);
}
