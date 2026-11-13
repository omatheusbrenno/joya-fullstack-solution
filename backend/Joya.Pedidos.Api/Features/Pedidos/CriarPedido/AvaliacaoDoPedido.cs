namespace Joya.Pedidos.Api.Features.Pedidos.CriarPedido;

/// <summary>Item de Pedido como chegou no request: só Produto e quantidade, sem preço (RN07).</summary>
public sealed record ItemSolicitado(int ProdutoId, int Quantidade);

/// <summary>Cliente carregado do banco para avaliar o Pedido.</summary>
public sealed record ClienteEncontrado(int Id, string Nome);

/// <summary>Produto carregado do banco, com o preço atual e o Estoque no momento da leitura.</summary>
public sealed record ProdutoEncontrado(int Id, string Nome, decimal Preco, int Estoque);

/// <summary>Item de Pedido com o Preço Unitário já congelado a partir do Produto.</summary>
public sealed record ItemMontado(int ProdutoId, string NomeProduto, int Quantidade, decimal PrecoUnitario);

/// <summary>Pedido que passou em todas as regras e está pronto para ser persistido.</summary>
public sealed record PedidoMontado(ClienteEncontrado Cliente, IReadOnlyList<ItemMontado> Itens)
{
    /// <summary>Σ quantidade × Preço Unitário (RN10). Calculado, nunca armazenado (ADR 0009).</summary>
    public decimal ValorTotal => Itens.Sum(item => item.Quantidade * item.PrecoUnitario);
}

/// <summary>Resultado de <see cref="RegrasDoPedido.Avaliar"/>: o Pedido montado ou as violações.</summary>
public abstract record AvaliacaoDoPedido;

public sealed record PedidoAceito(PedidoMontado Pedido) : AvaliacaoDoPedido;
