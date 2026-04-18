using Xunit;

namespace Tabatine.Worker.IntegrationTests;

[CollectionDefinition("SandboxCollection")]
public class SandboxCollection : ICollectionFixture<SandboxIntegrationTestWebAppFactory>
{
}
