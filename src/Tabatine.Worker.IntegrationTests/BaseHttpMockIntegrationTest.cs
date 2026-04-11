using Microsoft.Extensions.DependencyInjection;
using Respawn;
using Respawn.Graph;
using Xunit;

namespace Tabatine.Worker.IntegrationTests;

[CollectionDefinition("HttpMockCollection")]
public class HttpMockCollection : ICollectionFixture<HttpMockIntegrationTestWebAppFactory>
{
}

[Collection("HttpMockCollection")]
public abstract class BaseHttpMockIntegrationTest : IAsyncLifetime
{
    private readonly HttpMockIntegrationTestWebAppFactory _factory;
    private Respawner? _respawner;

    protected BaseHttpMockIntegrationTest(HttpMockIntegrationTestWebAppFactory factory)
    {
        _factory = factory;
        Client = factory.CreateClient();
    }

    protected HttpClient Client { get; }
    protected HttpMockIntegrationTestWebAppFactory Factory => _factory;

    public async Task InitializeAsync()
    {
        using var connection = new Npgsql.NpgsqlConnection(_factory.GetConnectionString());
        await connection.OpenAsync();

        if (_respawner == null)
        {
            _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
            {
                DbAdapter = DbAdapter.Postgres,
                SchemasToInclude = new[] { "public" },
                TablesToIgnore = new Table[] { "__EFMigrationsHistory" }
            });
        }

        await _respawner.ResetAsync(connection);
        
        // Limpar todos os mapeamentos do WireMock entre testes para evitar efeitos colaterais
        _factory.OmieMockServer.Reset();
    }

    public Task DisposeAsync() => Task.CompletedTask;
}
