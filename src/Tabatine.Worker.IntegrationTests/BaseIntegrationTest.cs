using NSubstitute;
using Respawn;
using Respawn.Graph;

namespace Tabatine.Worker.IntegrationTests;

[Collection("DatabaseCollection")]
public abstract class BaseIntegrationTest : IAsyncLifetime
{
    private readonly IntegrationTestWebAppFactory _factory;
    private Respawner? _respawner;

    protected BaseIntegrationTest(IntegrationTestWebAppFactory factory)
    {
        _factory = factory;
        Client = factory.CreateClient();
    }

    protected HttpClient Client { get; }
    protected IntegrationTestWebAppFactory Factory => _factory;

    public async Task InitializeAsync()
    {
        using var connection = new Npgsql.NpgsqlConnection(_factory.GetConnectionString());
        await connection.OpenAsync();

        // Configurar Respawn usando a connection string do container
        if (_respawner == null)
        {
            _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
            {
                DbAdapter = DbAdapter.Postgres,
                SchemasToInclude = new[] { "public" },
                TablesToIgnore = new Table[] { "__EFMigrationsHistory" }
            });
        }

        // Resetar o banco antes de cada teste
        await _respawner.ResetAsync(connection);

        // Limpar mocks compartilhados pelo Factory
        _factory.OmieClientMock.ClearReceivedCalls();
        _factory.NotificationServiceMock.ClearReceivedCalls();
    }

    public Task DisposeAsync() => Task.CompletedTask;
}
