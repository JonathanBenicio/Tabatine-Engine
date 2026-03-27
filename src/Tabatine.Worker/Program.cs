using Scalar.AspNetCore;
using Serilog;
using Tabatine.Worker.Endpoints;
using Tabatine.Worker.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Modularized Configurations
builder.AddCustomLogging();
builder.Services.AddOmieInfrastructure(builder.Configuration);
builder.Services.AddOmieClient(builder.Configuration);
builder.Services.AddCustomHealthChecks(builder.Configuration);
builder.Services.AddOpenApi();
builder.Services.AddHostedService<Tabatine.Worker.Services.WebhookProcessorWorker>();

var app = builder.Build();

// OpenAPI / Scalar
app.MapOpenApi();
app.MapScalarApiReference();

// Modularized Mappings
app.UseCustomHealthChecks();
app.MapOmieWebhookEndpoints();
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
