using System.ComponentModel.DataAnnotations;

namespace Joya.Pedidos.Api.Infraestrutura;

/// <summary>
/// O Período das consultas: datas inclusivas nas duas pontas, em UTC (RN12). No SQL ele vira o intervalo
/// semiaberto <c>[Inicio, FimExclusivo)</c> sobre DataPedido, sem função sobre a coluna, para usar o índice.
/// </summary>
public static class Periodo
{
    /// <summary>Meia-noite da data inicial.</summary>
    public static DateTime Inicio(DateOnly dataInicio) => dataInicio.ToDateTime(TimeOnly.MinValue);

    /// <summary>
    /// Meia-noite do dia seguinte à data final: a data final entra inteira, até 23:59:59.
    /// Na última data possível não há dia seguinte, e o fim vira o maior instante representável.
    /// </summary>
    public static DateTime FimExclusivo(DateOnly dataFim) =>
        dataFim == DateOnly.MaxValue ? DateTime.MaxValue : dataFim.AddDays(1).ToDateTime(TimeOnly.MinValue);

    /// <summary>Data final antes da inicial é erro de forma (RN12), apontado no campo informado.</summary>
    public static IEnumerable<ValidationResult> ValidarOrdem(DataIso? dataInicio, DataIso? dataFim, string campoDaDataFim)
    {
        if (dataInicio > dataFim)
        {
            yield return new ValidationResult("A data final deve ser igual ou posterior à data inicial.", [campoDaDataFim]);
        }
    }
}
