using Joya.Pedidos.Api.Features.Saude;
using Joya.Pedidos.Api.Infraestrutura;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(FabricaDeConexao.DaConfiguracao(builder.Configuration));
builder.Services.AddSaude();

var app = builder.Build();

app.MapSaude();

app.Run();
