namespace Joya.Pedidos.Api.Features.Pedidos.ListarPedidos;

/// <summary>Query string de GET /pedidos. O Período (dataInicio, dataFim) é inclusivo nas duas pontas, em UTC (RN12).</summary>
public sealed record ListarPedidosRequest(
    int? ClienteId,
    DateOnly? DataInicio,
    DateOnly? DataFim,
    int Pagina = 1,
    int TamanhoPagina = 20);
