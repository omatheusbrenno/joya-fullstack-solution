using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Joya.Pedidos.Tests.Integration.Infraestrutura;

/// <summary>Sobe a API em memória apontando para a connection string informada.</summary>
public sealed class ApiDeTestes(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Pedidos", connectionString);
    }
}
