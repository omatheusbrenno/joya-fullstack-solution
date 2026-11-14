namespace Joya.Pedidos.Api.Features.Relatorios.ConsultarTopClientes;

/// <summary>Uma linha do Top Clientes: o Valor Total é a soma dos Valores Totais dos Pedidos do Cliente no Período (ADR 0009).</summary>
public sealed record TopClienteResponse(
    int ClienteId,
    string Nome,
    int QuantidadePedidos,
    decimal ValorTotal);
