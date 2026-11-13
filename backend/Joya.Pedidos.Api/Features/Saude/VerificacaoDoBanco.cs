using Dapper;
using Joya.Pedidos.Api.Infraestrutura;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Joya.Pedidos.Api.Features.Saude;

/// <summary>A API só está saudável se consegue abrir conexão e consultar o banco.</summary>
public sealed class VerificacaoDoBanco(FabricaDeConexao fabricaDeConexao) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var conexao = await fabricaDeConexao.AbrirAsync(cancellationToken);
            await conexao.ExecuteScalarAsync<int>(new CommandDefinition("SELECT 1", cancellationToken: cancellationToken));
            return HealthCheckResult.Healthy();
        }
        catch (Exception excecao) when (excecao is Microsoft.Data.SqlClient.SqlException or InvalidOperationException)
        {
            // A descrição não vai para a resposta HTTP: só o status agregado é exposto.
            return HealthCheckResult.Unhealthy("Banco de dados indisponível.", excecao);
        }
    }
}
