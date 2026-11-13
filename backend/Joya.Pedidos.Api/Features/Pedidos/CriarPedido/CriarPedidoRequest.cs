namespace Joya.Pedidos.Api.Features.Pedidos.CriarPedido;

/// <summary>
/// Corpo de POST /pedidos. DTO explícito, sem campo de preço (RN07, OWASP A08): um "precoUnitario" enviado
/// é ignorado na desserialização (CA09). As regras de forma e de negócio ficam em <see cref="RegrasDoPedido"/>.
/// </summary>
public sealed record CriarPedidoRequest(int ClienteId, IReadOnlyList<ItemDoPedidoRequest>? Itens)
{
    public IReadOnlyList<ItemSolicitado> ItensSolicitados() =>
        [.. (Itens ?? []).Select(item => new ItemSolicitado(item.ProdutoId, item.Quantidade))];
}

public sealed record ItemDoPedidoRequest(int ProdutoId, int Quantidade);
