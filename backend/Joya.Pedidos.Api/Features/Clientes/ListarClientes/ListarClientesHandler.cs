using Dapper;
using Joya.Pedidos.Api.Infraestrutura;

namespace Joya.Pedidos.Api.Features.Clientes.ListarClientes;

public sealed record ClienteResponse(int Id, string Nome);

/// <summary>Todos os Clientes, para os seletores. Ordena por nome e, entre homônimos, por id.</summary>
public sealed class ListarClientesHandler(FabricaDeConexao fabricaDeConexao)
{
    public async Task<IReadOnlyList<ClienteResponse>> ExecutarAsync(CancellationToken cancellationToken)
    {
        await using var conexao = await fabricaDeConexao.AbrirAsync(cancellationToken);
        var clientes = await conexao.QueryAsync<ClienteResponse>(new CommandDefinition(
            "SELECT Id, Nome FROM dbo.Clientes ORDER BY Nome, Id;",
            cancellationToken: cancellationToken));
        return [.. clientes];
    }
}
