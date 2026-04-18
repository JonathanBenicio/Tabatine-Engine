using System.Text.Json;
using Tabatine.Core.Entities;
using Tabatine.Core.Interfaces;
using Tabatine.Infrastructure.Services;

namespace Tabatine.Worker.Services.Handlers;

/// <summary>
/// Webhook handler for Vendedor events.
/// Resolves event strings provided by Omie into actions over VendedorSyncService.
/// </summary>
public class VendedorWebhookHandler(VendedorSyncService vendedorSyncService, IEnumerable<INotificationService> notificationServices) : IWebhookEventHandler
{
    public IEnumerable<string> SupportedEvents => new[]
    {
        "Vendedor.Incluido",
        "Vendedor.Alterado",
        "Vendedor.Excluido"
    };

    public async Task HandleAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(webhookEvent.Payload)) return;

        using var doc = JsonDocument.Parse(webhookEvent.Payload);
        var root = doc.RootElement;
        
        long? codigoVendedor = null;
        if (root.TryGetProperty("nCodVend", out var el_n) && el_n.ValueKind == JsonValueKind.Number)
        {
            codigoVendedor = el_n.GetInt64();
        }
        else if (root.TryGetProperty("idVendedor", out var el0) && el0.ValueKind == JsonValueKind.Number)
        {
            codigoVendedor = el0.GetInt64();
        }
        else if (root.TryGetProperty("codigo", out var el1) && el1.ValueKind == JsonValueKind.Number)
        {
            codigoVendedor = el1.GetInt64();
        }
        else if (root.TryGetProperty("id_vendedor", out var el2) && el2.ValueKind == JsonValueKind.Number)
        {
            codigoVendedor = el2.GetInt64();
        }
        else if (root.TryGetProperty("codigo_vendedor", out var el3) && el3.ValueKind == JsonValueKind.Number)
        {
            codigoVendedor = el3.GetInt64();
        }

        if (codigoVendedor == null || codigoVendedor <= 0)
        {
            return; 
        }

        await vendedorSyncService.SyncByIdAsync(codigoVendedor.Value);
        
        string title = webhookEvent.Event switch 
        {
            "Vendedor.Incluido" => "Novo Vendedor",
            "Vendedor.Excluido" => "Vendedor Excluído",
            _ => "Vendedor Alterado"
        };
        
        var summary = $"Vendedor ID {codigoVendedor.Value} atualizado no ERP (Evento: {webhookEvent.Event}).";
        
        foreach (var service in notificationServices)
        {
            await service.SendNotificationAsync(title, summary, "VENDEDOR", codigoVendedor.Value);
        }
    }
}
