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

        // Resetar o banco antes de cada teste de forma limpa.
        // O WebhookProcessorWorker está rodando em paralelo, então o reset deve ser rápido.
        await _respawner.ResetAsync(connection);

        // Limpar mocks compartilhados pelo Factory para garantir um estado limpo
        _factory.OmieClientMock.ClearReceivedCalls();
        _factory.NotificationServiceMock.ClearReceivedCalls();
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
