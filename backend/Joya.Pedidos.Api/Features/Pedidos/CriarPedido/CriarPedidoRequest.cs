using System.ComponentModel.DataAnnotations;

namespace Joya.Pedidos.Api.Features.Pedidos.CriarPedido;

/// <summary>
/// Corpo de POST /pedidos. DTO explícito, sem campo de preço (RN07, OWASP A08): um "precoUnitario" enviado
/// é ignorado na desserialização (CA09). A borda (AddValidation) só barra o corpo que nem dá para avaliar:
/// Cliente ausente ou Item nulo. As regras do Pedido (RN01 a RN06) ficam em <see cref="RegrasDoPedido"/>.
/// </summary>
public sealed record CriarPedidoRequest(
    [property: Required(ErrorMessage = "Informe o Cliente.")] int? ClienteId,
    IReadOnlyList<ItemDoPedidoRequest?>? Itens)
    : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        for (var indice = 0; indice < (Itens?.Count ?? 0); indice++)
        {
            if (Itens![indice] is null)
            {
                yield return new ValidationResult("O Item não pode ser vazio.", [$"{nameof(Itens)}[{indice}]"]);
            }
        }
    }

    /// <summary>Só é chamado depois da validação da borda: Cliente presente e nenhum Item nulo.</summary>
    public IReadOnlyList<ItemSolicitado> ItensSolicitados() =>
        [.. (Itens ?? []).OfType<ItemDoPedidoRequest>().Select(item => new ItemSolicitado(item.ProdutoId, item.Quantidade))];
}

public sealed record ItemDoPedidoRequest(int ProdutoId, int Quantidade);
