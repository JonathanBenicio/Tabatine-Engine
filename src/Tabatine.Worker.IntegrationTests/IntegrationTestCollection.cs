using Xunit;

namespace Tabatine.Worker.IntegrationTests;

[CollectionDefinition("DatabaseCollection")]
public class DatabaseCollection : ICollectionFixture<IntegrationTestWebAppFactory>
{
    // Esta classe não possui código e serve apenas para aplicar o atributo [CollectionDefinition] 
    // e compartilhar o IntegrationTestWebAppFactory (e o PostgreSqlContainer) entre as classes de teste.
}
