using Microsoft.Data.SqlClient;

namespace Joya.Pedidos.Api.Infraestrutura;

/// <summary>
/// Abre conexões com o SQL Server da Gestão de Pedidos. É a única porta de entrada para o banco:
/// handlers recebem esta fábrica pelo construtor (dependência explícita, ADR 0002).
/// </summary>
public sealed class FabricaDeConexao(string connectionString)
{
    // O Azure SQL Database Free (ADR 0007) pausa o banco ocioso e, ao retomar, recusa logins por alguns segundos
    // com erros transitórios (40613, 40197, 4060...). A abertura tenta até 4 vezes, com espera crescente.
    // A lista de erros é a transitória padrão do SqlClient. Ela inclui o 4060 (banco não abre), então um nome de
    // banco errado também espera as tentativas antes de falhar; senha errada (18456) falha na hora.
    // Cada tentativa ainda respeita o Connect Timeout da connection string (15 s no deploy).
    private static readonly SqlRetryLogicBaseProvider Retentativa = SqlConfigurableRetryFactory.CreateExponentialRetryProvider(
        new SqlRetryLogicOption
        {
            NumberOfTries = 4,
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
