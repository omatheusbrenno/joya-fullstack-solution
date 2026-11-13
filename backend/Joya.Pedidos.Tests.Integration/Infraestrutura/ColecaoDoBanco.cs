namespace Joya.Pedidos.Tests.Integration.Infraestrutura;

[CollectionDefinition(Nome)]
public sealed class ColecaoDoBanco : ICollectionFixture<BancoDeTestes>
{
    public const string Nome = "Banco SQL Server";
}
