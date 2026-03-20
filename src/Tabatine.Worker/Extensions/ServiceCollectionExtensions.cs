using Microsoft.EntityFrameworkCore;
using Tabatine.Infrastructure.Data;
using Tabatine.Infrastructure.Services;
using Tabatine.Core.Interfaces;
using Tabatine.Infrastructure.Repositories;
using Tabatine.Omie.Client;
using Tabatine.Omie.Client.Models;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Tabatine.Worker.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOmieInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("DefaultConnection not found");

        // Configure DbContext
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                b => b.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)
                     .EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null)
                     .CommandTimeout(60)
            ).ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
        );

        // Configure Distributed Lock Service
        services.AddScoped<IDistributedLockService, DbDistributedLockService>();
        services.AddScoped<ISyncStateRepository, SyncStateRepository>();
        services.AddScoped<INotificationService, SupabaseNotificationService>();
        services.AddScoped<INotificationService, TelegramNotificationService>();

        // Sync Services
        services.AddScoped<ClienteSyncService>();
        services.AddScoped<ProdutoSyncService>();
        services.AddScoped<PedidoSyncService>();
        services.AddScoped<NotaFiscalSyncService>();
        services.AddScoped<VendedorSyncService>();
        services.AddScoped<ContaCorrenteSyncService>();
        services.AddScoped<BancoSyncService>();
        services.AddScoped<EtapaFaturamentoSyncService>();
        services.AddScoped<FormaPagamentoSyncService>();
        services.AddScoped<CondicaoPagamentoSyncService>();
        services.AddScoped<MeioPagamentoSyncService>();

        services.AddScoped<ISyncService, SyncManager>();
        services.AddHostedService<Worker>();

        return services;
    }

    public static IServiceCollection AddOmieClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OmieOptions>(configuration.GetSection("Omie"));

        services.AddHttpClient<IOmieClient, OmieClient>(client =>
        {
            var options = configuration.GetSection("Omie").Get<OmieOptions>();
            client.BaseAddress = new Uri(options?.BaseUrl ?? "https://app.omie.com.br/api/v1/");
        })
        .AddResilienceHandler("omie-pipeline", pipelineBuilder =>
        {
            pipelineBuilder.AddRetry(new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = 1,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                Delay = TimeSpan.FromSeconds(3),
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .HandleResult(r => (int)r.StatusCode >= 500)
            });

            pipelineBuilder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
            {
                SamplingDuration = TimeSpan.FromSeconds(60),
                FailureRatio = 0.8,
                MinimumThroughput = 20,
                BreakDuration = TimeSpan.FromSeconds(60)
            });

            pipelineBuilder.AddRateLimiter(new System.Threading.RateLimiting.FixedWindowRateLimiter(new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                PermitLimit = 200,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 50,
                QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst
            }));

            pipelineBuilder.AddConcurrencyLimiter(4);
            pipelineBuilder.AddTimeout(TimeSpan.FromSeconds(60));
        });

        return services;
    }
}
