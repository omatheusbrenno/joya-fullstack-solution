# Build e publish da API (.NET 10, ADR 0001).
FROM mcr.microsoft.com/dotnet/sdk:10.0.404-noble AS build
WORKDIR /src
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY Joya.Pedidos.Api/Joya.Pedidos.Api.csproj Joya.Pedidos.Api/
RUN dotnet restore Joya.Pedidos.Api/Joya.Pedidos.Api.csproj
COPY Joya.Pedidos.Api/ Joya.Pedidos.Api/
RUN dotnet publish Joya.Pedidos.Api/Joya.Pedidos.Api.csproj -c Release -o /app --no-restore

# Runtime chiseled: sem shell, sem gerenciador de pacotes, usuário não-root.
# Variante -extra porque o Microsoft.Data.SqlClient exige ICU (globalização).
FROM mcr.microsoft.com/dotnet/aspnet:10.0.15-noble-chiseled-extra AS runtime
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Joya.Pedidos.Api.dll"]
