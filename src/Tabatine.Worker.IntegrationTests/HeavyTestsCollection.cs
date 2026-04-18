using Xunit;

namespace Tabatine.Worker.IntegrationTests;

[CollectionDefinition("HeavyTestsCollection")]
public class HeavyTestsCollection : ICollectionFixture<IntegrationTestWebAppFactory>
{
    // Esta coleção isola testes pesados (como o Manual Sync) que bloqueiam o worker
    // por muito tempo, permitindo que outros testes de webhook rodem sem interferência.
}
