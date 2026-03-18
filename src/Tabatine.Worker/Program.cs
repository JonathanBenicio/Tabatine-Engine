using Tabatine.Worker;
using Microsoft.EntityFrameworkCore;
using Tabatine.Infrastructure.Data;
using Tabatine.Omie.Client.Models;
using Tabatine.Omie.Client;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Tabatine.Infrastructure.Services;
using Tabatine.Core.Interfaces;
using Tabatine.Infrastructure.Repositories;

var builder = Host.CreateApplicationBuilder(args);

// Configure DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)
    )
);

// Configure Omie Options
builder.Services.Configure<OmieOptions>(builder.Configuration.GetSection("Omie"));

// Configure Redis
builder.Services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(sp =>
{
    var configuration = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
    return StackExchange.Redis.ConnectionMultiplexer.Connect(configuration);
});

// Configure HttpClient with Resilience
builder.Services.AddHttpClient<IOmieClient, OmieClient>(client =>
{
    var options = builder.Configuration.GetSection("Omie").Get<OmieOptions>();
    client.BaseAddress = new Uri(options?.BaseUrl ?? "https://app.omie.com.br/api/v1/");
})
.AddResilienceHandler("omie-pipeline", pipelineBuilder =>
{
    pipelineBuilder.AddRetry(new HttpRetryStrategyOptions
    {
        MaxRetryAttempts = 1,  // Reduzido: 3 retries * N serviços tripavm o circuit breaker
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
        FailureRatio = 0.8,   // Abrir só quando 80% das chamadas falharem
        MinimumThroughput = 20, // Exige ao menos 20 chamadas antes de avaliar
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

builder.Services.AddScoped<ISyncStateRepository, SyncStateRepository>();
builder.Services.AddScoped<ClienteSyncService>();
builder.Services.AddScoped<ProdutoSyncService>();
builder.Services.AddScoped<PedidoSyncService>();
builder.Services.AddScoped<NotaFiscalSyncService>();
builder.Services.AddScoped<VendedorSyncService>();
builder.Services.AddScoped<ContaCorrenteSyncService>();
builder.Services.AddScoped<BancoSyncService>();
builder.Services.AddScoped<EtapaFaturamentoSyncService>();
builder.Services.AddScoped<FormaPagamentoSyncService>();

builder.Services.AddScoped<ISyncService, SyncManager>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();

// Migrate database on startup
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

host.Run();
