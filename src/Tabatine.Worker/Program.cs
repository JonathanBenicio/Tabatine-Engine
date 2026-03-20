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
using Microsoft.EntityFrameworkCore.Diagnostics;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.PostgreSQL;
using Serilog.Sinks.PostgreSQL.ColumnWriters;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;
using System.Net.Mime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("DefaultConnection not found");

Serilog.Debugging.SelfLog.Enable(msg => Console.WriteLine($"[SERILOG SELF-LOG] {msg}"));

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(
        restrictedToMinimumLevel: LogEventLevel.Debug,
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day, restrictedToMinimumLevel: LogEventLevel.Debug)
    .WriteTo.PostgreSQL(
        connectionString: connectionString,
        tableName: "\"Logs\"",
        schemaName: "public",
        needAutoCreateTable: false,
        restrictedToMinimumLevel: LogEventLevel.Information,
        columnOptions: new Dictionary<string, ColumnWriterBase>
        {
            { "message", new RenderedMessageColumnWriter() },
            { "message_template", new MessageTemplateColumnWriter() },
            { "level", new LevelColumnWriter() },
            { "timestamp", new TimestampColumnWriter() },
            { "exception", new ExceptionColumnWriter() },
            { "properties", new PropertiesColumnWriter() },
            { "log_event", new LogEventSerializedColumnWriter() }
        })
    .CreateLogger();

builder.Host.UseSerilog();

// Configure Health Checks
builder.Services.AddHealthChecks()
    .AddAsyncCheck("Database", async () =>
    {
        try
        {
            using var conn = new Npgsql.NpgsqlConnection(connectionString);
            await conn.OpenAsync();
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Erro ao conectar no banco", ex);
        }
    });

// Configure DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        connectionString,
        b => b.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)
             .EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null)
             .CommandTimeout(60)
    ).ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
);

// Configure Omie Options
builder.Services.Configure<OmieOptions>(builder.Configuration.GetSection("Omie"));

// Configure Distributed Lock Service
builder.Services.AddScoped<IDistributedLockService, DbDistributedLockService>();

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

builder.Services.AddScoped<ISyncStateRepository, SyncStateRepository>();
builder.Services.AddScoped<INotificationService, SupabaseNotificationService>();
builder.Services.AddScoped<INotificationService, TelegramNotificationService>();

builder.Services.AddScoped<ClienteSyncService>();
builder.Services.AddScoped<ProdutoSyncService>();
builder.Services.AddScoped<PedidoSyncService>();
builder.Services.AddScoped<NotaFiscalSyncService>();
builder.Services.AddScoped<VendedorSyncService>();
builder.Services.AddScoped<ContaCorrenteSyncService>();
builder.Services.AddScoped<BancoSyncService>();
builder.Services.AddScoped<EtapaFaturamentoSyncService>();
builder.Services.AddScoped<FormaPagamentoSyncService>();
builder.Services.AddScoped<CondicaoPagamentoSyncService>();
builder.Services.AddScoped<MeioPagamentoSyncService>();

builder.Services.AddScoped<ISyncService, SyncManager>();
builder.Services.AddHostedService<Worker>();

var app = builder.Build();

// Configure Health Check Endpoint with JSON output
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        var result = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            details = report.Entries.Select(e => new
            {
                key = e.Key,
                description = e.Value.Description,
                status = e.Value.Status.ToString(),
                error = e.Value.Exception?.Message
            })
        }, new JsonSerializerOptions { WriteIndented = true });

        context.Response.ContentType = MediaTypeNames.Application.Json;
        await context.Response.WriteAsync(result);
    }
});

// Omie Webhook Endpoint
app.MapPost("/webhook/omie", async (
    [FromBody] Tabatine.Worker.Models.OmieWebhookRequest request,
    IServiceProvider serviceProvider,
    ILogger<Program> logger) =>
{
    logger.LogInformation("Recebido Webhook Omie: Evento={Event}", request.Event);

    try
    {
        if (request.Message == null) return Results.Ok();

        using var scope = serviceProvider.CreateScope();
        var pedidoSync = scope.ServiceProvider.GetRequiredService<PedidoSyncService>();
        var nfSync = scope.ServiceProvider.GetRequiredService<NotaFiscalSyncService>();
        var notificationServices = scope.ServiceProvider.GetServices<INotificationService>();

        string? messageJson = request.Message.ToString();
        if (string.IsNullOrEmpty(messageJson)) return Results.Ok();

        if (request.Event == "VendaProduto.Novo" || request.Event == "VendaProduto.Alterado")
        {
            var pedidoMsg = JsonSerializer.Deserialize<Tabatine.Worker.Models.OmieWebhookPedidoMessage>(messageJson);
            if (pedidoMsg != null)
            {
                await pedidoSync.SyncByIdAsync(pedidoMsg.CodigoPedido);
                
                var title = request.Event == "VendaProduto.Novo" ? "Novo Pedido" : "Pedido Alterado";
                var summary = $"Pedido {pedidoMsg.NumeroPedido} - Etapa: {pedidoMsg.Etapa}";
                
                foreach (var service in notificationServices)
                {
                    await service.SendNotificationAsync(title, summary, "PEDIDO", pedidoMsg.CodigoPedido);
                }
            }
        }
        else if (request.Event == "Faturamento.NotaFiscalEmitida")
        {
            var nfMsg = JsonSerializer.Deserialize<Tabatine.Worker.Models.OmieWebhookNfMessage>(messageJson);
            if (nfMsg != null)
            {
                await nfSync.SyncByIdAsync(nfMsg.CodigoNf);
                
                var title = "NF Emitida";
                var summary = $"Nota Fiscal {nfMsg.NumeroNf} emitida.";
                
                foreach (var service in notificationServices)
                {
                    await service.SendNotificationAsync(title, summary, "NF", nfMsg.CodigoNf);
                }
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Erro ao processar webhook Omie");
    }

    return Results.Ok();
});

try
{
    // Migrate database on startup
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();
    }

    Log.Information("Aplicação Iniciada com Health Checks ativos.");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host encerrado inesperadamente.");
}
finally
{
    Log.CloseAndFlush();
}
