namespace Joya.Pedidos.Api.Features.Pedidos.ListarPedidos;

/// <summary>Uma linha da listagem. O Valor Total é calculado a partir dos Itens (ADR 0009).</summary>
public sealed record PedidoListadoResponse(
    int Id,
    int ClienteId,
    string NomeCliente,
    DateTime DataPedido,
    int QuantidadeItens,
    decimal ValorTotal);

public sealed record ListarPedidosResponse(
    IReadOnlyList<PedidoListadoResponse> Itens,
    int Pagina,
    int TamanhoPagina,
    int TotalItens);
