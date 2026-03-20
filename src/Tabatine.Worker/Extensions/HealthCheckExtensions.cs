using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;
using System.Net.Mime;

namespace Tabatine.Worker.Extensions;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddCustomHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("DefaultConnection not found");

        services.AddHealthChecks()
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

        return services;
    }

    public static void UseCustomHealthChecks(this IEndpointRouteBuilder app)
    {
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
    }
}
