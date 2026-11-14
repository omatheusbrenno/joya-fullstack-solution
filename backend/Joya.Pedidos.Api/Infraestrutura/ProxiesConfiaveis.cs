using Microsoft.AspNetCore.HttpOverrides;
using IPNetwork = System.Net.IPNetwork;

namespace Joya.Pedidos.Api.Infraestrutura;

/// <summary>
/// Forwarded headers (ADR 0011): atrás do nginx ou do proxy da plataforma, o IP do cliente chega em
/// X-Forwarded-For. A API só aceita esse header de quem está numa rede de "ProxiesConfiaveis" (CIDRs da
/// configuração, além do loopback padrão). De qualquer outra origem ele é ignorado, e o rate limit usa o IP
/// da conexão. Com ForwardLimit = 1, vale só a última entrada, a que o proxy confiável acrescentou: o que o
/// cliente escreveu antes dela não conta.
/// </summary>
public static class ProxiesConfiaveis
{
    public static IServiceCollection AddProxiesConfiaveis(this IServiceCollection services, IConfiguration configuration)
    {
        // CIDR inválido derruba a subida: melhor falhar fechado do que confiar em qualquer um.
        var redes = (configuration.GetSection("ProxiesConfiaveis").Get<string[]>() ?? []).Select(IPNetwork.Parse).ToList();

        return services.Configure<ForwardedHeadersOptions>(opcoes =>
        {
            opcoes.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            opcoes.ForwardLimit = 1;
            redes.ForEach(opcoes.KnownIPNetworks.Add);
        });
    }
}
