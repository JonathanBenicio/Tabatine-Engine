using Scalar.AspNetCore;
using Serilog;
using Tabatine.Worker.Endpoints;
using Tabatine.Worker.Extensions;

var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
if (isDevelopment)
{
    var envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env.local");
    if (File.Exists(envPath))
    {
        foreach (var line in File.ReadAllLines(envPath))
        {
            var parts = line.Split('=', 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2)
            {
                Environment.SetEnvironmentVariable(parts[0].Trim(), parts[1].Trim());
                Console.WriteLine($"[ENV] {parts[0].Trim()} carregado.");
            }
        }
    }
}

var builder = WebApplication.CreateBuilder(args);

// Modularized Configurations
builder.AddCustomLogging();
builder.Services.AddOmieInfrastructure(builder.Configuration);
builder.Services.AddOmieClient(builder.Configuration);
builder.Services.AddCustomHealthChecks(builder.Configuration);
builder.Services.AddOpenApi();
builder.Services.AddMemoryCache();
builder.Services.AddHostedService<Tabatine.Worker.Services.WebhookProcessorWorker>();

var app = builder.Build();

app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
});

// OpenAPI / Scalar
app.MapOpenApi();
app.MapScalarApiReference();

// Modularized Mappings
app.UseCustomHealthChecks();
app.MapOmieWebhookEndpoints();
app.MapTelegramWebhookEndpoints();
app.MapSyncEndpoints();
app.ApplyMigrations();

try
{
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

public partial class Program { }
