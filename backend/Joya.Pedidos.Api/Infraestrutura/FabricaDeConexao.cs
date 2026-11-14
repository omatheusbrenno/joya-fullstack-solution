using Microsoft.Data.SqlClient;

namespace Joya.Pedidos.Api.Infraestrutura;

/// <summary>
/// Abre conexões com o SQL Server da Gestão de Pedidos. É a única porta de entrada para o banco:
/// handlers recebem esta fábrica pelo construtor (dependência explícita, ADR 0002).
/// </summary>
public sealed class FabricaDeConexao(string connectionString)
{
    // O Azure SQL Database Free (ADR 0007) pausa o banco ocioso e, ao retomar, recusa logins por alguns segundos
    // com erros transitórios (40613, 40197...). A abertura tenta de novo, com espera crescente, por cerca de 30 s.
    // A lista de erros é a transitória padrão do SqlClient: senha errada ou banco inexistente falham na hora.
    private static readonly SqlRetryLogicBaseProvider Retentativa = SqlConfigurableRetryFactory.CreateExponentialRetryProvider(
        new SqlRetryLogicOption
        {
            NumberOfTries = 5,
            DeltaTime = TimeSpan.FromSeconds(2),
            MaxTimeInterval = TimeSpan.FromSeconds(15),
        });

    public async Task<SqlConnection> AbrirAsync(CancellationToken cancellationToken)
    {
        var conexao = new SqlConnection(connectionString) { RetryLogicProvider = Retentativa };
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
