using Microsoft.Extensions.DependencyInjection;
using Respawn;
using Respawn.Graph;
using Xunit;

namespace Tabatine.Worker.IntegrationTests;

[Collection("SandboxCollection")]
public abstract class BaseSandboxIntegrationTest : IAsyncLifetime
{
    private readonly SandboxIntegrationTestWebAppFactory _factory;
    private Respawner? _respawner;

    protected BaseSandboxIntegrationTest(SandboxIntegrationTestWebAppFactory factory)
    {
        _factory = factory;
        Client = factory.CreateClient();
    }

    protected HttpClient Client { get; }
    protected SandboxIntegrationTestWebAppFactory Factory => _factory;

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
    }

    public Task DisposeAsync() => Task.CompletedTask;
    
    // Método auxiliar para criar o Helper do Sandbox resolvendo do config
    protected Helpers.SandboxOmieHelper CreateOmieHelper()
    {
        using var scope = _factory.Services.CreateScope();
        var config = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
        
        var appKey = config["Omie:Sandbox:AppKey"];
        var appSecret = config["Omie:Sandbox:AppSecret"];
        var baseUrl = config["Omie:Sandbox:BaseUrl"];
        
        return new Helpers.SandboxOmieHelper(appKey!, appSecret!, baseUrl!);
    }
}
