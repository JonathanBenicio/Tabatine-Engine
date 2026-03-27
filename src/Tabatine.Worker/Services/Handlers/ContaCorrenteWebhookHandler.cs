using System.Text.Json;
using Tabatine.Core.Entities;
using Tabatine.Core.Interfaces;
using Tabatine.Infrastructure.Services;

namespace Tabatine.Worker.Services.Handlers;

/// <summary>
/// Webhook handler for Financas.ContaCorrente events.
/// Resolves event strings provided by Omie into actions over ContaCorrenteSyncService.
/// </summary>
public class ContaCorrenteWebhookHandler(ContaCorrenteSyncService contaCorrenteSyncService, IEnumerable<INotificationService> notificationServices) : IWebhookEventHandler
{
    public IEnumerable<string> SupportedEvents => new[]
    {
        "Financas.ContaCorrente.Incluido",
        "Financas.ContaCorrente.Alterado",
        "Financas.ContaCorrente.Excluido"
    };

    public async Task HandleAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(webhookEvent.Payload)) return;

        using var doc = JsonDocument.Parse(webhookEvent.Payload);
        var root = doc.RootElement;
        
        long? codigoCC = null;
        if (root.TryGetProperty("nCodCC", out var el1) && el1.ValueKind == JsonValueKind.Number)
        {
            codigoCC = el1.GetInt64();
        }
        else if (root.TryGetProperty("id_conta_corrente", out var el2) && el2.ValueKind == JsonValueKind.Number)
        {
            codigoCC = el2.GetInt64();
        }

        if (codigoCC == null || codigoCC <= 0)
        {
            return; 
        }

        await contaCorrenteSyncService.SyncByIdAsync(codigoCC.Value);
        
        string title = webhookEvent.Event switch 
        {
            "Financas.ContaCorrente.Incluido" => "Nova Conta Corrente",
            "Financas.ContaCorrente.Excluido" => "Conta Corrente Excluída",
            _ => "Conta Corrente Alterada"
        };
        
        var summary = $"Conta Corrente ID {codigoCC.Value} atualizada no ERP (Evento: {webhookEvent.Event}).";
        
        foreach (var service in notificationServices)
        {
            await service.SendNotificationAsync(title, summary, "CONTA_CORRENTE", codigoCC.Value);
        }
    }
}
