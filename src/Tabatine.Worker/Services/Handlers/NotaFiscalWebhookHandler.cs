using System.Text.Json;
using Tabatine.Core.Entities;
using Tabatine.Core.Interfaces;
using Tabatine.Infrastructure.Services;
using Tabatine.Worker.Models;

namespace Tabatine.Worker.Services.Handlers;

/// <summary>
/// Webhook handler for NotaFiscal events.
/// Resolves event strings provided by Omie into actions over NotaFiscalSyncService.
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

        var nfMsg = JsonSerializer.Deserialize<OmieWebhookNfMessage>(webhookEvent.Payload);
        if (nfMsg == null) return;

        await nfSyncService.SyncByIdAsync(nfMsg.CodigoNf);
        
        string title = webhookEvent.Event switch 
        {
            "Faturamento.NotaFiscalEmitida" => "NF Emitida",
            "NFe.NotaAutorizada" => "NF-e Autorizada",
            "NFe.NotaCancelada" => "NF-e Cancelada",
            "NFSe.NotaAutorizada" => "NFS-e Autorizada",
            "NFSe.NotaCancelada" => "NFS-e Cancelada",
            _ => "Nota Fiscal Atualizada"
        };
        
        var summary = $"Nota Fiscal {nfMsg.NumeroNf} atualizada no ERP (Evento: {webhookEvent.Event}).";
        
        foreach (var service in notificationServices)
        {
            await service.SendNotificationAsync(title, summary, "NF", nfMsg.CodigoNf);
        }
    }
}
