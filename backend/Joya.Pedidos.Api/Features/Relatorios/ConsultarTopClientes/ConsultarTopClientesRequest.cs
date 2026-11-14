using System.ComponentModel.DataAnnotations;
using Joya.Pedidos.Api.Infraestrutura;

namespace Joya.Pedidos.Api.Features.Relatorios.ConsultarTopClientes;

/// <summary>
/// Query string de GET /relatorios/top-clientes, validada na borda pelo AddValidation (400 em ProblemDetails, ADR 0008).
/// O Período é inclusivo nas duas pontas, em UTC (RN12), e obrigatório (RN13).
/// </summary>
public sealed record ConsultarTopClientesRequest(
    [property: Required(ErrorMessage = "Informe a data inicial do Período.")] DataIso? DataInicio,
    [property: Required(ErrorMessage = "Informe a data final do Período.")] DataIso? DataFim)
    : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) =>
        Periodo.ValidarOrdem(DataInicio, DataFim, nameof(DataFim));
}
