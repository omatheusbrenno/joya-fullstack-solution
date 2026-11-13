using System.Text.Json;

namespace Joya.Pedidos.Api.Infraestrutura;

/// <summary>
/// ProblemDetails da API (ADR 0008). A validação nativa nomeia os erros pela propriedade C# ("DataFim");
/// aqui as chaves passam para camelCase, o mesmo nome do campo no JSON e na query string ("dataFim").
/// </summary>
public static class ProblemDetailsDaApi
{
    public static IServiceCollection AddProblemDetailsDaApi(this IServiceCollection services) =>
        services.AddProblemDetails(opcoes => opcoes.CustomizeProblemDetails = contexto =>
        {
            if (contexto.ProblemDetails is HttpValidationProblemDetails validacao)
            {
                validacao.Errors = validacao.Errors.ToDictionary(erro => NomeDoCampo(erro.Key), erro => erro.Value);
            }
        });

    /// <summary>"Itens[0].Quantidade" vira "itens[0].quantidade".</summary>
    internal static string NomeDoCampo(string caminho) =>
        string.Join('.', caminho.Split('.').Select(JsonNamingPolicy.CamelCase.ConvertName));
}
