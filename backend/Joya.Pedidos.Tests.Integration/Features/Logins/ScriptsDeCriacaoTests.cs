using Joya.Pedidos.Tests.Integration.Infraestrutura;
using Microsoft.Data.SqlClient;

namespace Joya.Pedidos.Tests.Integration.Features.Logins;

/// <summary>
/// Os scripts de banco e de login rodam em todo <c>docker compose up</c> e de novo no deploy (ADR 0007), então
/// aplicá-los outra vez num banco que já existe não pode falhar nem tirar o acesso dos logins.
/// </summary>
[Collection(ColecaoDoBanco.Nome)]
public sealed class ScriptsDeCriacaoTests(BancoDeTestes banco)
{
    [Fact]
    public async Task Reaplicar_banco_e_logins_mantem_a_API_e_o_MCP_conectando()
    {
        await banco.AplicarScriptAsync("00-criar-banco.sql");
        await banco.AplicarScriptAsync("02-criar-login-api.sql");
        await banco.AplicarScriptAsync("05-criar-login-mcp.sql");

        Assert.Equal(1, await ContarAsync(banco.ConnectionString));
        Assert.Equal(1, await ContarAsync(banco.ConnectionStringMcp));
    }

    private static async Task<int> ContarAsync(string connectionString)
    {
        var cancelamento = TestContext.Current.CancellationToken;
        await using var conexao = new SqlConnection(connectionString);
        await conexao.OpenAsync(cancelamento);
        await using var comando = new SqlCommand("SELECT 1;", conexao);
        return (int)(await comando.ExecuteScalarAsync(cancelamento))!;
    }
}
