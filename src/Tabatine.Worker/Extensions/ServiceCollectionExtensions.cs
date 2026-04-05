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
using Tabatine.Worker.Services.Handlers;

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
                     .UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)
            )
            .UseSnakeCaseNamingConvention()
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
        );

        // Configure Distributed Lock Service
        services.AddScoped<IDistributedLockService, DbDistributedLockService>();
        services.AddScoped<ISyncStateRepository, SyncStateRepository>();
        services.AddScoped<INotificationService, SupabaseNotificationService>();

        // Telegram: registado como concreto (para o endpoint de webhook poder resolver directamente)
        // e também como INotificationService (para broadcast via ISyncService)
        services.AddHttpClient<TelegramNotificationService>();
        services.AddScoped<INotificationService>(sp => sp.GetRequiredService<TelegramNotificationService>());

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
        services.AddScoped<ContasReceberSyncService>();
        services.AddScoped<ContasPagarSyncService>();

        // Webhook Handlers
        services.AddScoped<IWebhookEventHandler, PedidoWebhookHandler>();
        services.AddScoped<IWebhookEventHandler, NotaFiscalWebhookHandler>();
        services.AddScoped<IWebhookEventHandler, ClienteWebhookHandler>();
        services.AddScoped<IWebhookEventHandler, ProdutoWebhookHandler>();
        services.AddScoped<IWebhookEventHandler, VendedorWebhookHandler>();
        services.AddScoped<IWebhookEventHandler, ContaCorrenteWebhookHandler>();
        services.AddScoped<IWebhookEventHandler, SystemManualSyncWebhookHandler>();
        services.AddScoped<WebhookHandlerFactory>();

        services.AddScoped<ISyncService, SyncManager>();

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
                MaxRetryAttempts = 3,
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
