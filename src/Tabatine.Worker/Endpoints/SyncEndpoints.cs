using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tabatine.Infrastructure.Data;
using Tabatine.Core.Entities;
using System.Text.Json;

namespace Tabatine.Worker.Endpoints;

public static class SyncEndpoints
{
    public static void MapSyncEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/sync/trigger", async ([FromServices] AppDbContext dbContext) =>
        {
            var syncEvent = new WebhookEvent
            {
                AppKey = "SYSTEM",
                Event = "System.ManualSync",
                Payload = "{}",
                Status = "Pending"
            };

            dbContext.WebhookEvents.Add(syncEvent);
            await dbContext.SaveChangesAsync();

            return Results.Accepted("/api/sync/trigger", new { Message = "Sincronização completa (Manual Sync) foi agendada e será processada em background pela fila.", EventId = syncEvent.Id });
        })
        .WithName("TriggerSystemManualSync")
        .WithDescription("Agenda uma sincronização completa de todos os módulos Omie na Fila do Supabase.");
    }
}
