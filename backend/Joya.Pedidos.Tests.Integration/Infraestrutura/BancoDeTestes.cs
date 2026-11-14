using Testcontainers.MsSql;

namespace Joya.Pedidos.Tests.Integration.Infraestrutura;

/// <summary>
/// SQL Server real (Testcontainers) com os mesmos scripts de <c>database/</c> aplicados (ADR 0006).
/// Um container por execução da suíte, compartilhado entre as classes de teste.
/// A API conecta com o login de privilégio mínimo (joya_api), como no compose.
/// O login do MCP (joya_mcp, somente leitura, ADR 0010) também é criado, para provar que ele não escreve.
/// </summary>
public sealed class BancoDeTestes : IAsyncLifetime
{
    private const string NomeDoBanco = "JoyaPedidos";
    private const string LoginDaApi = "joya_api";
    private const string SenhaDaApi = "Testes_Integracao_2026!";
    private const string LoginDoMcp = "joya_mcp";
    private const string SenhaDoMcp = "Testes_Leitura_2026!";

    private static readonly string[] ScriptsDeCriacao = ["00-criar-banco.sql", "01-criar-schema.sql", "02-criar-login-api.sql", "05-criar-login-mcp.sql"];

    private readonly MsSqlContainer _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-CU28-ubuntu-22.04")
        .Build();

    /// <summary>Conexão da API (joya_api, privilégio mínimo).</summary>
    public string ConnectionString { get; private set; } = string.Empty;

    /// <summary>Conexão do MCP (joya_mcp, somente leitura + SHOWPLAN).</summary>
    public string ConnectionStringMcp { get; private set; } = string.Empty;

    /// <summary>Conexão administrativa, só para preparar cenários (arrange), nunca para verificar resultado.</summary>
    public string ConnectionStringAdmin { get; private set; } = string.Empty;

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        foreach (var script in ScriptsDeCriacao)
        {
            await AplicarScriptAsync(script);
        }

        ConnectionStringAdmin = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(_container.GetConnectionString())
        {
            InitialCatalog = NomeDoBanco,
        }.ConnectionString;

        ConnectionString = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(_container.GetConnectionString())
        {
            InitialCatalog = NomeDoBanco,
            UserID = LoginDaApi,
            Password = SenhaDaApi,
        }.ConnectionString;

        ConnectionStringMcp = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(_container.GetConnectionString())
        {
            InitialCatalog = NomeDoBanco,
            UserID = LoginDoMcp,
            Password = SenhaDoMcp,
        }.ConnectionString;
    }

    /// <summary>Aplica um script de <c>database/</c> como o db-init do compose faz (usuário sa).</summary>
    public async Task AplicarScriptAsync(string script)
    {
        // O ExecScriptAsync não repassa variáveis do sqlcmd: a senha entra aqui, como o db-init faz com -v.
        var conteudo = (await File.ReadAllTextAsync(Path.Combine(LocalizarPastaDatabase(), script)))
            .Replace("$(SENHA_API)", SenhaDaApi, StringComparison.Ordinal)
            .Replace("$(SENHA_MCP)", SenhaDoMcp, StringComparison.Ordinal);
        var resultado = await _container.ExecScriptAsync(conteudo);
        if (resultado.ExitCode != 0)
        {
            throw new InvalidOperationException($"Falha ao aplicar {script}: {resultado.Stderr}{resultado.Stdout}");
        }
    }

    public async ValueTask DisposeAsync() => await _container.DisposeAsync();

    private static string LocalizarPastaDatabase()
    {
        var diretorio = new DirectoryInfo(AppContext.BaseDirectory);
        while (diretorio is not null && !Directory.Exists(Path.Combine(diretorio.FullName, "database")))
        {
            diretorio = diretorio.Parent;
        }

        return diretorio is null
            ? throw new DirectoryNotFoundException("Pasta database/ não encontrada acima do diretório de testes.")
            : Path.Combine(diretorio.FullName, "database");
    }
}
