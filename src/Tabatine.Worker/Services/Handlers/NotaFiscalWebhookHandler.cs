using System.Text.Json;
using Tabatine.Core.Entities;
using Tabatine.Core.Interfaces;
using Tabatine.Infrastructure.Services;

namespace Tabatine.Worker.Services.Handlers;

/// <summary>
/// Webhook handler for NotaFiscal events.
/// Supports both legacy fields (codigo_nf) and Connect 2.0 fields (idNf).
/// </summary>
public class NotaFiscalWebhookHandler(NotaFiscalSyncService nfSyncService, IEnumerable<INotificationService> notificationServices) : IWebhookEventHandler
{

    public IEnumerable<string> SupportedEvents => new[]
    {
        "Faturamento.NotaFiscalEmitida", // Old format
        "NFe.NotaAutorizada",
        "NFe.NotaCancelada",
        "NFe.NotaDevolucaoAutorizada",
        "NFSe.NotaAutorizada",
        "NFSe.NotaCancelada",
        "NFSe.NotaSubstituida"
    };

    public async Task HandleAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(webhookEvent.Payload)) return;

        using var doc = JsonDocument.Parse(webhookEvent.Payload);
        var root = doc.RootElement;

        // Extract NF ID — Connect 2.0 uses "idNf", legacy uses "codigo_nf"
        long? codigoNf = null;
        if (root.TryGetProperty("idNf", out var el1) && el1.ValueKind == JsonValueKind.Number)
        {
            codigoNf = el1.GetInt64();
        }
        else if (root.TryGetProperty("codigo_nf", out var el2) && el2.ValueKind == JsonValueKind.Number)
        {
            codigoNf = el2.GetInt64();
        }

        if (codigoNf == null || codigoNf <= 0) return;

        // Extract numero NF for notification
        string? numeroNf = null;
        if (root.TryGetProperty("numeroNf", out var nn1) && nn1.ValueKind == JsonValueKind.String)
        {
            numeroNf = nn1.GetString();
        }
        else if (root.TryGetProperty("numero_nf", out var nn2) && nn2.ValueKind == JsonValueKind.String)
        {
            numeroNf = nn2.GetString();
        }

        await nfSyncService.SyncByIdAsync(codigoNf.Value);
        
        string title = webhookEvent.Event switch 
        {
            "Faturamento.NotaFiscalEmitida" => "NF Emitida",
            "NFe.NotaAutorizada" => "NF-e Autorizada",
            "NFe.NotaCancelada" => "NF-e Cancelada",
            "NFSe.NotaAutorizada" => "NFS-e Autorizada",
            "NFSe.NotaCancelada" => "NFS-e Cancelada",
            _ => "Nota Fiscal Atualizada"
        };
        
        var summary = $"Nota Fiscal {numeroNf ?? codigoNf.ToString()} atualizada no ERP (Evento: {webhookEvent.Event}).";
        
        foreach (var service in notificationServices)
        {
            await service.SendNotificationAsync(title, summary, "NF", codigoNf.Value);
        }
    }
}
