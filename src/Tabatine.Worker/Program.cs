using Tabatine.Worker;
using Microsoft.EntityFrameworkCore;
using Tabatine.Infrastructure.Data;
using Tabatine.Omie.Client.Models;
using Tabatine.Omie.Client;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Tabatine.Infrastructure.Services;
using Tabatine.Core.Interfaces;

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
        MaxRetryAttempts = 3,
        BackoffType = DelayBackoffType.Exponential,
        UseJitter = true,
        Delay = TimeSpan.FromSeconds(2)
    });

    pipelineBuilder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
    {
        SamplingDuration = TimeSpan.FromSeconds(30),
        FailureRatio = 0.5,
        MinimumThroughput = 10,
        BreakDuration = TimeSpan.FromSeconds(30)
    });

    pipelineBuilder.AddConcurrencyLimiter(4);
    
    pipelineBuilder.AddTimeout(TimeSpan.FromSeconds(60));
});

builder.Services.AddScoped<ClienteSyncService>();
builder.Services.AddScoped<ProdutoSyncService>();
builder.Services.AddScoped<PedidoSyncService>();
builder.Services.AddScoped<NotaFiscalSyncService>();

builder.Services.AddScoped<ISyncService, SyncManager>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
