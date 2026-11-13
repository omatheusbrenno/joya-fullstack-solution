using Testcontainers.MsSql;

namespace Joya.Pedidos.Tests.Integration.Infraestrutura;

/// <summary>
/// SQL Server real (Testcontainers) com os mesmos scripts de <c>database/</c> aplicados (ADR 0006).
/// Um container por execução da suíte, compartilhado entre as classes de teste.
/// </summary>
public sealed class BancoDeTestes : IAsyncLifetime
{
    private const string NomeDoBanco = "JoyaPedidos";

    private static readonly string[] ScriptsDeCriacao = ["00-criar-banco.sql", "01-criar-schema.sql"];

    private readonly MsSqlContainer _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-CU28-ubuntu-22.04")
        .Build();

    public string ConnectionString { get; private set; } = string.Empty;

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        foreach (var script in ScriptsDeCriacao)
        {
            var conteudo = await File.ReadAllTextAsync(Path.Combine(LocalizarPastaDatabase(), script));
            var resultado = await _container.ExecScriptAsync(conteudo);
            if (resultado.ExitCode != 0)
            {
                throw new InvalidOperationException($"Falha ao aplicar {script}: {resultado.Stderr}{resultado.Stdout}");
            }
        }

        ConnectionString = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(_container.GetConnectionString())
        {
            InitialCatalog = NomeDoBanco,
        }.ConnectionString;
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
