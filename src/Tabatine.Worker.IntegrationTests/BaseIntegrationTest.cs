using NSubstitute;
using NSubstitute.ClearExtensions;
using Respawn;
using Respawn.Graph;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tabatine.Infrastructure.Data;
using Tabatine.Core.Entities;
using System.Diagnostics;

namespace Tabatine.Worker.IntegrationTests;

[Collection("DatabaseCollection")]
[Trait("Category", "Integrated")]
public abstract class BaseIntegrationTest : IAsyncLifetime
{
    private static Respawner? _respawner;
    private static readonly SemaphoreSlim _respawnerLock = new(1, 1);
    private readonly IntegrationTestWebAppFactory _factory;

    protected const int DefaultPollingTimeoutSeconds = 30;

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

        // Tentar drenar a fila de webhooks para evitar que eventos de testes anteriores interfiram
        await WaitForWebhookQueueToDrainAsync(TimeSpan.FromSeconds(5));

        // Inicialização thread-safe do Respawner (Singleton para a suite)
        if (_respawner == null)
        {
            await _respawnerLock.WaitAsync();
            try
            {
                if (_respawner == null)
                {
                    _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
                    {
                        DbAdapter = DbAdapter.Postgres,
                        SchemasToInclude = new[] { "public" },
                        TablesToIgnore = new Table[] { "__EFMigrationsHistory" }
                    });
                }
            }
            finally
            {
                _respawnerLock.Release();
            }
        }

        // Resetar o banco antes de cada teste de forma limpa
        await _respawner.ResetAsync(connection);

        // Limpar mocks compartilhados pelo Factory para garantir um estado limpo
        _factory.OmieClientMock.ClearReceivedCalls();
        _factory.NotificationServiceMock.ClearReceivedCalls();
    }

    protected async Task WaitForWebhookQueueToDrainAsync(TimeSpan timeout)
    {
        try 
        {
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Espera até o timeout para que a fila fique vazia
            var sw = Stopwatch.StartNew();
            while (sw.Elapsed < timeout)
            {
                var pendingCount = await dbContext.WebhookEvents
                    .CountAsync(w => w.Status == WebhookEvent.StatusPending || w.Status == WebhookEvent.StatusProcessing);

                if (pendingCount == 0) break;
                await Task.Delay(200);
            }
        }
        catch 
        {
            // Silenciosamente falha se o banco não estiver pronto ou a tabela não existir
        }
    }

    protected async Task WaitForConditionAsync(Func<Task<bool>> condition, string errorMessage = "Tempo esgotado aguardando condição.")
    {
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed.TotalSeconds < DefaultPollingTimeoutSeconds)
        {
            if (await condition()) return;
            await Task.Delay(500);
        }
        throw new TimeoutException(errorMessage);
    }

    public Task DisposeAsync() => Task.CompletedTask;
}
