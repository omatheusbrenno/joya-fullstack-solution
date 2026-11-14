using Joya.Pedidos.Tests.Integration.Infraestrutura;
using Microsoft.Data.SqlClient;

namespace Joya.Pedidos.Tests.Integration.Features.Mcp;

/// <summary>
/// O Claude lê o banco pelo MCP com o login joya_mcp (ADR 0010). A garantia de que o agente não altera dados
/// está na permissão do login, não na flag "read-only" do servidor MCP: estes testes provam isso pelo próprio login.
/// </summary>
[Collection(ColecaoDoBanco.Nome)]
public sealed class LoginDoMcpTests(BancoDeTestes banco) : IAsyncLifetime
{
    // Erro 229 do SQL Server: "The %ls permission was denied on the object...".
    private const int PermissaoNegada = 229;

    private readonly CenarioDoBanco _cenario = new(banco);

    public async ValueTask InitializeAsync() => await _cenario.LimparAsync();

    public async ValueTask DisposeAsync() => await _cenario.LimparAsync();

    [Fact]
    public async Task Insert_falha_por_permissao()
    {
        var erro = await Assert.ThrowsAsync<SqlException>(
            () => ExecutarComoMcpAsync("INSERT INTO dbo.Clientes (Nome) VALUES (N'Intruso');"));

        Assert.Equal(PermissaoNegada, erro.Number);
    }

    [Fact]
    public async Task Delete_falha_por_permissao_e_o_Cliente_continua_la()
    {
        await _cenario.InserirClienteAsync("Ana Souza");

        var erro = await Assert.ThrowsAsync<SqlException>(
            () => ExecutarComoMcpAsync("DELETE FROM dbo.Clientes;"));

        Assert.Equal(PermissaoNegada, erro.Number);
        Assert.Equal(1, await ExecutarComoMcpAsync("SELECT COUNT(*) FROM dbo.Clientes;"));
    }

    [Fact]
    public async Task Le_o_plano_de_execucao_estimado()
    {
        var cancelamento = TestContext.Current.CancellationToken;
        await using var conexao = new SqlConnection(banco.ConnectionStringMcp);
        await conexao.OpenAsync(cancelamento);

        // SET SHOWPLAN_XML precisa ser o único comando do lote: a query seguinte devolve o plano em vez das linhas.
        await using (var ligar = new SqlCommand("SET SHOWPLAN_XML ON;", conexao))
        {
            await ligar.ExecuteNonQueryAsync(cancelamento);
        }

        await using var consulta = new SqlCommand("SELECT Id, Nome FROM dbo.Clientes;", conexao);
        var plano = (string?)await consulta.ExecuteScalarAsync(cancelamento);

        Assert.StartsWith("<ShowPlanXML", plano, StringComparison.Ordinal);
    }

    private async Task<object?> ExecutarComoMcpAsync(string sql)
    {
        var cancelamento = TestContext.Current.CancellationToken;
        await using var conexao = new SqlConnection(banco.ConnectionStringMcp);
        await conexao.OpenAsync(cancelamento);
        await using var comando = new SqlCommand(sql, conexao);
        return await comando.ExecuteScalarAsync(cancelamento);
    }
}
