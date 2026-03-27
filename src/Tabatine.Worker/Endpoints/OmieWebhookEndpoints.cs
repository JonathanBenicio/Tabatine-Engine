using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Tabatine.Core.Interfaces;
using Tabatine.Infrastructure.Services;
using Tabatine.Worker.Models;

namespace Tabatine.Worker.Endpoints;

public static class OmieWebhookEndpoints
{
    public static void MapOmieWebhookEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/webhook/omie", async (
            [FromBody] OmieWebhookRequest request,
            IServiceProvider serviceProvider,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("OmieWebhook");
            logger.LogInformation("Recebido Webhook Omie: Evento={Event}", request.Event);

            try
            {
                if (request.Message == null) return Results.Ok();

                string? messageJson = request.Message.ToString();
                if (string.IsNullOrEmpty(messageJson)) return Results.Ok();

                // Salvar o evento na fila para processamento em background (Fast Acknowledge)
                using var scope = serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<Tabatine.Infrastructure.Data.AppDbContext>();

                var webhookEvent = new Tabatine.Core.Entities.WebhookEvent
                {
                    AppKey = request.AppKey ?? string.Empty,
                    Event = request.Event ?? "Desconhecido",
                    Payload = messageJson,
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow
                };

                await dbContext.WebhookEvents.AddAsync(webhookEvent);
                await dbContext.SaveChangesAsync();
                
                logger.LogInformation("Evento {Event} salvo na fila com ID {Id}", request.Event, webhookEvent.Id);
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
