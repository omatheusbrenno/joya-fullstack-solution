using Joya.Pedidos.Api.Features.Clientes.ListarClientes;
using Joya.Pedidos.Api.Features.Saude;
using Joya.Pedidos.Api.Infraestrutura;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(FabricaDeConexao.DaConfiguracao(builder.Configuration));
builder.Services.AddSaude();
builder.Services.AddListarClientes();

var app = builder.Build();

app.MapSaude();
app.MapListarClientes();

app.Run();
