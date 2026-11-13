using Microsoft.Data.SqlClient;

namespace Joya.Pedidos.Api.Infraestrutura;

/// <summary>
/// Abre conexões com o SQL Server da Gestão de Pedidos. É a única porta de entrada para o banco:
/// handlers recebem esta fábrica pelo construtor (dependência explícita, ADR 0002).
/// </summary>
public sealed class FabricaDeConexao(string connectionString)
{
    public async Task<SqlConnection> AbrirAsync(CancellationToken cancellationToken)
    {
        var conexao = new SqlConnection(connectionString);
        await conexao.OpenAsync(cancellationToken);
        return conexao;
    }

    public static FabricaDeConexao DaConfiguracao(IConfiguration configuracao)
    {
        var connectionString = configuracao.GetConnectionString("Pedidos");
        return string.IsNullOrWhiteSpace(connectionString)
            ? throw new InvalidOperationException("ConnectionStrings:Pedidos não configurada. Defina a variável de ambiente ConnectionStrings__Pedidos.")
            : new FabricaDeConexao(connectionString);
    }
}
