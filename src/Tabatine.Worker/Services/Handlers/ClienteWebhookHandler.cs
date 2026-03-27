using System.Text.Json;
using Tabatine.Core.Entities;
using Tabatine.Core.Interfaces;
using Tabatine.Infrastructure.Services;

namespace Tabatine.Worker.Services.Handlers;

/// <summary>
/// Webhook handler for ClienteFornecedor events.
/// Resolves event strings provided by Omie into actions over ClienteSyncService.
/// </summary>
public class ClienteWebhookHandler(ClienteSyncService clienteSyncService, IEnumerable<INotificationService> notificationServices) : IWebhookEventHandler
{

    public IEnumerable<string> SupportedEvents => new[]
    {
        "ClienteFornecedor.Incluido",
        "ClienteFornecedor.Alterado",
        "ClienteFornecedor.Excluido"
    };

    public async Task HandleAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(webhookEvent.Payload)) return;

        using var doc = JsonDocument.Parse(webhookEvent.Payload);
        var root = doc.RootElement;
        
        // Extract the code regardless of the exact schema provided we find the property
        long? codigoCliente = null;
        if (root.TryGetProperty("codigo_cliente_omie", out var el1) && el1.ValueKind == JsonValueKind.Number)
        {
            codigoCliente = el1.GetInt64();
        }
        else if (root.TryGetProperty("codigo_cliente_fornecedor", out var el2) && el2.ValueKind == JsonValueKind.Number)
        {
            codigoCliente = el2.GetInt64();
        }

        if (codigoCliente == null || codigoCliente <= 0)
        {
            return; // Needs an ID to sync
        }

        await clienteSyncService.SyncByIdAsync(codigoCliente.Value);
        
        string title = webhookEvent.Event switch 
        {
            "ClienteFornecedor.Incluido" => "Novo Cliente/Fornecedor",
            "ClienteFornecedor.Excluido" => "Cliente/Fornecedor Excluído",
            _ => "Cliente/Fornecedor Alterado"
        };
        
        var summary = $"Cliente/Fornecedor ID {codigoCliente.Value} atualizado (Evento: {webhookEvent.Event}).";
        
        foreach (var service in notificationServices)
        {
            await service.SendNotificationAsync(title, summary, "CLIENTE", codigoCliente.Value);
        }
    }
}
