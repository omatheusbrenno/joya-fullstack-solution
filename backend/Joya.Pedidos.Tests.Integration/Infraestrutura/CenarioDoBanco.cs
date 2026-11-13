using Dapper;
using Microsoft.Data.SqlClient;

namespace Joya.Pedidos.Tests.Integration.Infraestrutura;

/// <summary>
/// Monta o estado do banco antes de cada teste (arrange), com a conexão administrativa.
/// As classes da coleção do banco rodam em série, então cada teste começa de um banco vazio.
/// </summary>
public sealed class CenarioDoBanco(BancoDeTestes banco)
{
    public async Task LimparAsync()
    {
        await using var conexao = await AbrirAsync();
        await conexao.ExecuteAsync("""
            DELETE FROM dbo.ItensPedido;
            DELETE FROM dbo.Pedidos;
            DELETE FROM dbo.Produtos;
            DELETE FROM dbo.Clientes;
            """);
    }

    public async Task<int> InserirClienteAsync(string nome)
    {
        await using var conexao = await AbrirAsync();
        return await conexao.ExecuteScalarAsync<int>(
            "INSERT INTO dbo.Clientes (Nome) OUTPUT INSERTED.Id VALUES (@nome);", new { nome });
    }

    public async Task<int> InserirProdutoAsync(string nome, decimal preco, int estoque = 100)
    {
        await using var conexao = await AbrirAsync();
        return await conexao.ExecuteScalarAsync<int>(
            "INSERT INTO dbo.Produtos (Nome, Preco, Estoque) OUTPUT INSERTED.Id VALUES (@nome, @preco, @estoque);",
            new { nome, preco, estoque });
    }

    /// <summary>Insere um Pedido já pronto (Data do Pedido em UTC) com seus Itens.</summary>
    public async Task<int> InserirPedidoAsync(int clienteId, DateTime dataPedido, params (int ProdutoId, int Quantidade, decimal PrecoUnitario)[] itens)
    {
        await using var conexao = await AbrirAsync();
        var pedidoId = await conexao.ExecuteScalarAsync<int>(
            "INSERT INTO dbo.Pedidos (ClienteId, DataPedido) OUTPUT INSERTED.Id VALUES (@clienteId, @dataPedido);",
            new { clienteId, dataPedido });
        foreach (var (produtoId, quantidade, precoUnitario) in itens)
        {
            await conexao.ExecuteAsync(
                "INSERT INTO dbo.ItensPedido (PedidoId, ProdutoId, Quantidade, PrecoUnitario) VALUES (@pedidoId, @produtoId, @quantidade, @precoUnitario);",
                new { pedidoId, produtoId, quantidade, precoUnitario });
        }

        return pedidoId;
    }

    private async Task<SqlConnection> AbrirAsync()
    {
        var conexao = new SqlConnection(banco.ConnectionStringAdmin);
        await conexao.OpenAsync();
        return conexao;
    }
}
