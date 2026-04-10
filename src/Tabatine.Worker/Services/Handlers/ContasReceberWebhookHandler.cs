using System.Text.Json;
using Tabatine.Core.Entities;
using Tabatine.Core.Interfaces;
using Tabatine.Infrastructure.Services;

namespace Tabatine.Worker.Services.Handlers;

/// <summary>
/// Webhook handler for ContaReceber events.
/// </summary>
public class ContasReceberWebhookHandler(ContasReceberSyncService syncService, IEnumerable<INotificationService> notificationServices) : IWebhookEventHandler
{
    public IEnumerable<string> SupportedEvents => new[]
    {
        "Financas.ContaReceber.Incluido",
        "Financas.ContaReceber.Alterado",
        "Financas.ContaReceber.Excluido"
    };

    public async Task HandleAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(webhookEvent.Payload)) return;

        using var doc = JsonDocument.Parse(webhookEvent.Payload);
        var root = doc.RootElement;
        
        long? omieId = null;
        if (root.TryGetProperty("nCodLanc", out var el) && el.ValueKind == JsonValueKind.Number)
        {
            omieId = el.GetInt64();
        }
        else if (root.TryGetProperty("codigo_lancamento_omie", out var el2) && el2.ValueKind == JsonValueKind.Number)
        {
            omieId = el2.GetInt64();
        }

        if (omieId == null || omieId <= 0) return;

        string title;
        string summary;

        if (webhookEvent.Event.EndsWith(".Excluido", StringComparison.OrdinalIgnoreCase))
        {
            await syncService.CancelByIdAsync(omieId.Value, cancellationToken);
            title = "Conta a Receber Excluída (Cancelada)";
            summary = $"Título de Receber ID {omieId.Value} marcado como CANCELADO localmente.";
        }
        else
        {
            await syncService.SyncByIdAsync(omieId.Value, cancellationToken);
            title = webhookEvent.Event.EndsWith(".Incluido") ? "Nova Conta a Receber" : "Conta a Receber Alterada";
            summary = $"Título de Receber ID {omieId.Value} sincronizado com sucesso.";
        }

        foreach (var service in notificationServices)
        {
            await service.SendNotificationAsync(title, summary, "FINANCEIRO", omieId.Value);
        }
    }
}
