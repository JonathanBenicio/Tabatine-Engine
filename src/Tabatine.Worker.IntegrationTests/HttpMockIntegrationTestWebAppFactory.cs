using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Tabatine.Core.Interfaces;
using Tabatine.Infrastructure.Data;
using Tabatine.Omie.Client;
using Tabatine.Omie.Client.Models;
using Testcontainers.PostgreSql;
using WireMock.Server;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Tabatine.Worker.IntegrationTests;

public class HttpMockIntegrationTestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder("postgres:16-alpine")
        .Build();

    public WireMockServer OmieMockServer { get; private set; } = null!;
    public INotificationService NotificationServiceMock { get; } = Substitute.For<INotificationService>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "WebhookProcessor:PollingIntervalMs", "100" },
                { "WebhookProcessor:UseSkipLocked", "false" },
                { "Omie:BaseUrl", OmieMockServer.Url }, // Aponta para o WireMock
                { "Omie:AppKey", "MOCK_KEY" },
                { "Omie:AppSecret", "MOCK_SECRET" }
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Banco de dados (Testcontainers)
            services.RemoveAll<IDbContextFactory<AppDbContext>>();
            services.RemoveAll<AppDbContext>();
            services.RemoveAll<DbContextOptions<AppDbContext>>();

            var connectionString = _dbContainer.GetConnectionString() + ";Pooling=false;No Reset On Close=true;GssEncryptionMode=Disable";
            
            services.AddDbContextFactory<AppDbContext>(options =>
            {
                options.UseNpgsql(connectionString)
                       .UseSnakeCaseNamingConvention();
            });

            services.AddScoped(sp => sp.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext());

            // Notification Service permanece mockado por interface (não é o foco aqui)
            services.RemoveAll<INotificationService>();
            services.AddSingleton(NotificationServiceMock);

            // NÃO removemos o IOmieClient. O DI vai usar a implementação real registrada no Program.cs
            // mas configurada com a URL do WireMock via AppConfiguration.
        });
    }

    public async Task InitializeAsync()
    {
        OmieMockServer = WireMockServer.Start();
        await _dbContainer.StartAsync();

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        OmieMockServer.Stop();
        await _dbContainer.StopAsync();
    }

    public string GetConnectionString() => _dbContainer.GetConnectionString();
}
