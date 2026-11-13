using Dapper;
using Joya.Pedidos.Api.Infraestrutura;

namespace Joya.Pedidos.Api.Features.Produtos.ListarProdutos;

public sealed record ProdutoResponse(int Id, string Nome, decimal Preco, int Estoque);

/// <summary>Todos os Produtos com preço atual e Estoque, para o modal de criação de Pedido. Ordena por nome e id.</summary>
public sealed class ListarProdutosHandler(FabricaDeConexao fabricaDeConexao)
{
    public async Task<IReadOnlyList<ProdutoResponse>> ExecutarAsync(CancellationToken cancellationToken)
    {
        await using var conexao = await fabricaDeConexao.AbrirAsync(cancellationToken);
        var produtos = await conexao.QueryAsync<ProdutoResponse>(new CommandDefinition(
            "SELECT Id, Nome, Preco, Estoque FROM dbo.Produtos ORDER BY Nome, Id;",
            cancellationToken: cancellationToken));
        return [.. produtos];
    }
}
