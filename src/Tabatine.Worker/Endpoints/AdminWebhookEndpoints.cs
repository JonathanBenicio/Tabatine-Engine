using Microsoft.EntityFrameworkCore;
using Tabatine.Infrastructure.Data;
using Tabatine.Core.Entities;

namespace Tabatine.Worker.Endpoints;

public static class AdminWebhookEndpoints
{
    public static void MapAdminWebhookEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/admin/webhooks")
                       .WithTags("Admin - Webhooks");

        // 1. Listar Eventos (Paginado, sem payload)
        group.MapGet("/", async (
            AppDbContext dbContext,
            string? status,
            string? eventName,
            int page = 1,
            int pageSize = 20) =>
        {
            var query = dbContext.WebhookEvents.AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(e => e.Status == status);

            if (!string.IsNullOrEmpty(eventName))
                query = query.Where(e => e.Event == eventName);

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(e => e.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(e => new 
                {
                    e.Id,
                    e.AppKey,
                    e.Event,
                    e.Status,
                    e.RetryCount,
                    e.MaxRetries,
                    e.CreatedAt,
                    e.ProcessedAt,
                    e.LastAttemptAt,
                    e.NextRetryAt,
                    e.LastErrorDetail,
                    e.MessageId
                })
                .ToListAsync();

            return Results.Ok(new
            {
                items,
                total,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling(total / (double)pageSize)
            });
        });

        // 2. Buscar Evento por ID (com Payload)
        group.MapGet("/{id:guid}", async (AppDbContext dbContext, Guid id) =>
        {
            var ev = await dbContext.WebhookEvents.FindAsync(id);
            return ev is null ? Results.NotFound() : Results.Ok(ev);
        });

        // 3. Re-processar Evento (Retry Manual)
        group.MapPost("/{id:guid}/retry", async (AppDbContext dbContext, Guid id) =>
        {
            var ev = await dbContext.WebhookEvents.FindAsync(id);
            if (ev is null) return Results.NotFound();
            
            if (ev.Status == "Processing") 
                return Results.Conflict(new { message = "Evento já está sendo processado." });

            ev.Status = "Pending";
            ev.RetryCount = 0;
            ev.NextRetryAt = null;
            ev.LastErrorDetail = null;

            await dbContext.SaveChangesAsync();

            return Results.Accepted(null, new 
            { 
                id = ev.Id, 
                message = "Evento enfileirado para reprocessamento",
                newStatus = "Pending" 
            });
        });

        // 4. Descartar Evento (Soft Delete)
        group.MapDelete("/{id:guid}", async (AppDbContext dbContext, Guid id) =>
        {
            var ev = await dbContext.WebhookEvents.FindAsync(id);
            if (ev is null) return Results.NotFound();

            if (ev.Status == "Processing")
                return Results.Conflict(new { message = "Não é possível descartar um evento em processamento." });

            ev.Status = "Dismissed";
            await dbContext.SaveChangesAsync();

            return Results.NoContent();
        });

        // 5. Estatísticas
        group.MapGet("/stats", async (AppDbContext dbContext) =>
        {
            var stats = await dbContext.WebhookEvents
                .GroupBy(e => e.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            var completedToday = await dbContext.WebhookEvents
                .Where(e => e.Status == "Completed" && e.ProcessedAt >= DateTime.UtcNow.Date)
                .CountAsync();

            var lastEvent = await dbContext.WebhookEvents
                .OrderByDescending(e => e.CreatedAt)
                .Select(e => e.CreatedAt)
                .FirstOrDefaultAsync();

            return Results.Ok(new
            {
                pending = stats.FirstOrDefault(s => s.Status == "Pending")?.Count ?? 0,
                processing = stats.FirstOrDefault(s => s.Status == "Processing")?.Count ?? 0,
                failed = stats.FirstOrDefault(s => s.Status == "Failed")?.Count ?? 0,
                deadLetter = stats.FirstOrDefault(s => s.Status == "DeadLetter")?.Count ?? 0,
                completedToday,
                lastEventAt = lastEvent
            });
        });
    }
}
