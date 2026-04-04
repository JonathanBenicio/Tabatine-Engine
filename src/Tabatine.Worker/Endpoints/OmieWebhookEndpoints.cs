using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tabatine.Infrastructure.Data;
using Tabatine.Worker.Models;

namespace Tabatine.Worker.Endpoints;

public static class OmieWebhookEndpoints
{
    public static void MapOmieWebhookEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/webhook/omie", async (
            HttpContext httpContext,
            IServiceProvider serviceProvider,
            ILoggerFactory loggerFactory,
            Serilog.IDiagnosticContext diagnosticContext) =>
        {
            var logger = loggerFactory.CreateLogger("OmieWebhook");

            try
            {
                // Deserializar como modelo unificado
                var request = await httpContext.Request.ReadFromJsonAsync<OmieWebhookRequest>();
                if (request == null)
                {
                    logger.LogWarning("Payload do webhook Omie é nulo ou inválido.");
                    return Results.Ok();
                }

                var eventName = request.ResolvedEventName;
                var payload = request.ResolvedPayload;

                if (!string.IsNullOrEmpty(eventName))
                {
                    diagnosticContext.Set("Event", eventName);
                }

                if (!string.IsNullOrEmpty(request.MessageId))
                {
                    diagnosticContext.Set("MessageId", request.MessageId);
                }

                logger.LogInformation("Recebido Webhook Omie: Formato={Format}, Evento={Event}, MessageId={MessageId}",
                    request.IsConnect2 ? "Connect2.0" : "Legado",
                    eventName,
                    request.MessageId);

                if (string.IsNullOrEmpty(eventName) || string.IsNullOrEmpty(payload))
                {
                    logger.LogWarning("Webhook recebido sem evento ou payload válido. Ignorando.");
                    return Results.Ok();
                }

                // Fast Acknowledge: salvar na fila para processamento em background
                using var scope = serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Idempotência: verificar se já recebemos este messageId (Connect 2.0)
                if (!string.IsNullOrEmpty(request.MessageId))
                {
                    var alreadyExists = await dbContext.WebhookEvents
                        .AnyAsync(e => e.MessageId == request.MessageId);

                    if (alreadyExists)
                    {
                        logger.LogInformation("Webhook duplicado ignorado. MessageId={MessageId}", request.MessageId);
                        return Results.Ok();
                    }
                }

                var webhookEvent = new Tabatine.Core.Entities.WebhookEvent
                {
                    AppKey = request.AppKey ?? string.Empty,
                    Event = eventName,
                    Payload = payload,
                    Status = "Pending",
                    MessageId = request.MessageId,
                    CreatedAt = DateTime.UtcNow
                };

                await dbContext.WebhookEvents.AddAsync(webhookEvent);
                await dbContext.SaveChangesAsync();

                logger.LogInformation("Evento {Event} salvo na fila com ID {Id}", eventName, webhookEvent.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao gravar webhook Omie na fila");
            }

            // Sempre retornar 200 OK rapidamente para não bloquear a fila da Omie
            return Results.Ok();
        });
    }
}
