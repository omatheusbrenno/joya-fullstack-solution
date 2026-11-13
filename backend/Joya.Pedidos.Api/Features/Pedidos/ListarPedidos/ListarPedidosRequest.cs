using System.ComponentModel.DataAnnotations;
using Joya.Pedidos.Api.Infraestrutura;

namespace Joya.Pedidos.Api.Features.Pedidos.ListarPedidos;

/// <summary>
/// Query string de GET /pedidos, validada na borda pelo AddValidation (400 em ProblemDetails, ADR 0008).
/// O Período (dataInicio, dataFim) é inclusivo nas duas pontas, em UTC (RN12).
/// </summary>
public sealed record ListarPedidosRequest(
    int? ClienteId,
    DataIso? DataInicio,
    DataIso? DataFim,
    [property: Range(1, int.MaxValue, ErrorMessage = "A página deve ser maior ou igual a 1.")] int Pagina = 1,
    [property: Range(1, ListarPedidosRequest.TamanhoMaximoDaPagina, ErrorMessage = "O tamanho da página deve estar entre 1 e 100.")] int TamanhoPagina = 20)
    : IValidatableObject
{
    public const int TamanhoMaximoDaPagina = 100;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DataInicio > DataFim)
        {
            yield return new ValidationResult("A data final deve ser igual ou posterior à data inicial.", [nameof(DataFim)]);
        }
    }
}
