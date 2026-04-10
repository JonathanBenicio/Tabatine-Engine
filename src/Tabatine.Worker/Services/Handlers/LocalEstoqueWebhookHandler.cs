using System.Text.Json;
using Tabatine.Core.Entities;
using Tabatine.Core.Interfaces;
using Tabatine.Infrastructure.Services;

namespace Tabatine.Worker.Services.Handlers;

/// <summary>
/// Webhook handler for LocalEstoque events.
/// </summary>
public class LocalEstoqueWebhookHandler(EstoqueSyncService estoqueSyncService, IEnumerable<INotificationService> notificationServices) : IWebhookEventHandler
{
    public IEnumerable<string> SupportedEvents => new[]
    {
        "LocalEstoque.Incluido",
        "LocalEstoque.Alterado",
        "LocalEstoque.Excluido"
    };

    public async Task HandleAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(webhookEvent.Payload)) return;

        using var doc = JsonDocument.Parse(webhookEvent.Payload);
        var root = doc.RootElement;
        
        long? omieId = null;
        if (root.TryGetProperty("codigo_local_estoque", out var el) && el.ValueKind == JsonValueKind.Number)
        {
            omieId = el.GetInt64();
        }

        if (omieId == null || omieId <= 0) return;

        // Locais de estoque são poucos, sincronizamos todos para garantir hierarquia/padrão, 
        // ou usamos o novo método individual que implementamos.
        await estoqueSyncService.SyncLocalByIdAsync(omieId.Value, cancellationToken);
        
        string title = webhookEvent.Event switch 
        {
            "Estoque.LocalEstoque.Incluido" => "Novo Local de Estoque",
            "Estoque.LocalEstoque.Excluido" => "Local de Estoque Excluído",
            _ => "Local de Estoque Alterado"
        };
        
        var summary = $"Local de Estoque ID {omieId.Value} atualizado.";
        
        foreach (var service in notificationServices)
        {
            await service.SendNotificationAsync(title, summary, "ESTOQUE", omieId.Value);
        }
    }
}
