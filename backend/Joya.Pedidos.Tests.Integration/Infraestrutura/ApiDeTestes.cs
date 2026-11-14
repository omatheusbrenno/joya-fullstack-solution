using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Joya.Pedidos.Tests.Integration.Infraestrutura;

/// <summary>
/// Sobe a API em memória apontando para a connection string informada. O TestServer não tem conexão de rede,
/// então toda requisição chega como se viesse de <paramref name="ipRemoto"/>; por padrão, o <see cref="Proxy"/>
/// confiável, como o nginx do compose (ADR 0011).
/// </summary>
public sealed class ApiDeTestes(string connectionString, IPAddress? ipRemoto = null) : WebApplicationFactory<Program>
{
    public static readonly IPAddress Proxy = IPAddress.Parse("10.0.0.1");

    /// <summary>Banco que não responde: o handler lança SqlException, um erro que nenhum slice trata.</summary>
    public const string BancoInalcancavel = "Server=127.0.0.1,1;Database=JoyaPedidos;User Id=sa;Password=SenhaSecreta;Connect Timeout=1;TrustServerCertificate=True";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Pedidos", connectionString);
        builder.UseSetting("ProxiesConfiaveis:0", $"{Proxy}/32");
        builder.ConfigureServices(services => services.AddSingleton<IStartupFilter>(new IpRemotoFixo(ipRemoto ?? Proxy)));
    }

    private sealed class IpRemotoFixo(IPAddress ip) : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use((contexto, proximo) =>
            {
                contexto.Connection.RemoteIpAddress = ip;
                return proximo(contexto);
            });
            next(app);
        };
    }
}
