using Joya.Pedidos.Api.Features.Clientes.ListarClientes;
using Joya.Pedidos.Api.Features.Pedidos.ListarPedidos;
using Joya.Pedidos.Api.Features.Produtos.ListarProdutos;
using Joya.Pedidos.Api.Features.Saude;
using Joya.Pedidos.Api.Infraestrutura;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(FabricaDeConexao.DaConfiguracao(builder.Configuration));
builder.Services.AddProblemDetailsDaApi();
builder.Services.AddValidation();
builder.Services.AddSaude();
builder.Services.AddListarClientes();
builder.Services.AddListarPedidos();
builder.Services.AddListarProdutos();

var app = builder.Build();

// Respostas de erro sem corpo (ex.: query string que não converte para o tipo) saem em ProblemDetails (ADR 0008).
app.UseStatusCodePages();

app.MapSaude();
app.MapListarClientes();
app.MapListarPedidos();
app.MapListarProdutos();

app.Run();
