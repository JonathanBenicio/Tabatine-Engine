using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;

namespace Tabatine.Worker.IntegrationTests;

public class IntegrationTestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder("postgres:16-alpine")
        .Build();

    public IOmieClient OmieClientMock { get; } = Substitute.For<IOmieClient>();
    public INotificationService NotificationServiceMock { get; } = Substitute.For<INotificationService>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "WebhookProcessor:PollingIntervalMs", "100" },
                { "WebhookProcessor:UseSkipLocked", "false" }
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Remover registros relacionados ao banco de dados real
            services.RemoveAll<IDbContextFactory<AppDbContext>>();
            services.RemoveAll<AppDbContext>();
            services.RemoveAll<DbContextOptions<AppDbContext>>();

            // Adicionar AppDbContextFactory com Testcontainers (Npgsql)
            // IMPORTANTE: Pooling=false e No Reset On Close=true conforme AGENTS.md para evitar problemas com Supavisor/Postgres
            var connectionString = _dbContainer.GetConnectionString() + ";Pooling=false;No Reset On Close=true;GssEncryptionMode=Disable";
            
            services.AddDbContextFactory<AppDbContext>(options =>
            {
                options.UseNpgsql(connectionString)
                       .UseSnakeCaseNamingConvention();
            });

            // Registrar o AppDbContext resolvido a partir da Factory, conforme feito no Program.cs
            services.AddScoped(sp => sp.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext());

            // Substituir IOmieClient e INotificationService por Mocks
            services.RemoveAll<IOmieClient>();
            services.AddSingleton(OmieClientMock);

            services.RemoveAll<INotificationService>();
            services.AddSingleton(NotificationServiceMock);
        });
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        // Aplicar migrations após o container subir
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await _dbContainer.StopAsync();
    }

    // Método auxiliar para obter a connection string real do container para o Respawn
    public string GetConnectionString() => _dbContainer.GetConnectionString();
}
