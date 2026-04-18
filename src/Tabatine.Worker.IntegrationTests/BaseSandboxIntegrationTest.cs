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
        
        var appKey = GetConfigValue(config, "Omie:AppKey_Sandbox", "Omie:Sandbox:AppKey");
        var appSecret = GetConfigValue(config, "Omie:AppSecret_Sandbox", "Omie:Sandbox:AppSecret");
        var baseUrl = GetConfigValue(config, "Omie:BaseUrl_Sandbox", "Omie:Sandbox:BaseUrl");
        
        return new Helpers.SandboxOmieHelper(appKey!, appSecret!, baseUrl!);
    }

    private static string? GetConfigValue(Microsoft.Extensions.Configuration.IConfiguration config, params string[] keys)
    {
        foreach (var key in keys)
        {
            var value = config[key];
            if (!string.IsNullOrEmpty(value) && value != "Use-Environment-Variable")
            {
                return value;
            }
        }
        return null;
    }
}
