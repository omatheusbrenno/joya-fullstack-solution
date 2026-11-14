using Joya.Pedidos.Api.Features.Clientes.ListarClientes;
using Joya.Pedidos.Api.Features.Pedidos.CriarPedido;
using Joya.Pedidos.Api.Features.Pedidos.ListarPedidos;
using Joya.Pedidos.Api.Features.Produtos.ListarProdutos;
using Joya.Pedidos.Api.Features.Relatorios.ConsultarTopClientes;
using Joya.Pedidos.Api.Features.Saude;
using Joya.Pedidos.Api.Infraestrutura;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(FabricaDeConexao.DaConfiguracao(builder.Configuration));
builder.Services.AddProblemDetailsDaApi();
builder.Services.AddValidation();
builder.Services.AddProxiesConfiaveis(builder.Configuration);
builder.Services.AddLimiteDeRequisicoes();
builder.Services.AddSaude();
builder.Services.AddListarClientes();
builder.Services.AddListarPedidos();
builder.Services.AddCriarPedido();
builder.Services.AddListarProdutos();
builder.Services.AddConsultarTopClientes();

var app = builder.Build();

// Primeiro o IP real do cliente: tudo que vem depois (rate limit, logs) já o enxerga.
app.UseForwardedHeaders();

// Exceção não tratada vira 500 em ProblemDetails genérico, sem stack trace nem mensagem interna (CA18, ADR 0008).
// O middleware registra a exceção no log. A página de erro detalhada fica só em Development.
app.UseExceptionHandler();

// Respostas de erro sem corpo (ex.: query string que não converte para o tipo) saem em ProblemDetails (ADR 0008).
app.UseStatusCodePages();
app.UseRouting();
app.UseRateLimiter();
app.UseLimiteDeCorpo();

app.MapSaude();
app.MapListarClientes();
app.MapListarPedidos();
app.MapCriarPedido();
app.MapListarProdutos();
app.MapConsultarTopClientes();

app.Run();
