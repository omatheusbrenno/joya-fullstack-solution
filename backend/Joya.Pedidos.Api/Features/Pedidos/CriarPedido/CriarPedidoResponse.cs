namespace Joya.Pedidos.Api.Features.Pedidos.CriarPedido;

/// <summary>Corpo do 201 de POST /pedidos. O Valor Total é calculado a partir dos Itens (ADR 0009).</summary>
public sealed record CriarPedidoResponse(
    int Id,
    int ClienteId,
    string NomeCliente,
    DateTime DataPedido,
    IReadOnlyList<ItemDoPedidoResponse> Itens,
    decimal ValorTotal);

public sealed record ItemDoPedidoResponse(int ProdutoId, string NomeProduto, int Quantidade, decimal PrecoUnitario);

/// <summary>O que o handler devolve: o Pedido criado ou a recusa com todas as violações.</summary>
public abstract record ResultadoDeCriarPedido;

public sealed record PedidoCriado(CriarPedidoResponse Pedido) : ResultadoDeCriarPedido;

public sealed record PedidoNaoCriado(IReadOnlyList<Violacao> Violacoes) : ResultadoDeCriarPedido;
