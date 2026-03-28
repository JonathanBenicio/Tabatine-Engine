using System.Text.Json;
using Tabatine.Core.Entities;
using Tabatine.Core.Interfaces;
using Tabatine.Infrastructure.Services;

namespace Tabatine.Worker.Services.Handlers;

/// <summary>
/// Webhook handler for VendaProduto events.
/// Supports both legacy fields (codigo_pedido_omie) and Connect 2.0 fields (idPedido).
/// </summary>
public class PedidoWebhookHandler(PedidoSyncService pedidoSyncService, IEnumerable<INotificationService> notificationServices) : IWebhookEventHandler
{

    public IEnumerable<string> SupportedEvents => new[]
    {
        "VendaProduto.Novo", // Old format
        "VendaProduto.Incluida",
        "VendaProduto.Alterada",
        "VendaProduto.Cancelada",
        "VendaProduto.Devolvida",
        "VendaProduto.EtapaAlterada",
        "VendaProduto.Excluida",
        "VendaProduto.Faturada"
    };

    public async Task HandleAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(webhookEvent.Payload)) return;

        using var doc = JsonDocument.Parse(webhookEvent.Payload);
        var root = doc.RootElement;

        // Extract pedido ID — Connect 2.0 uses "idPedido", legacy uses "codigo_pedido_omie"
        long? codigoPedido = null;
        if (root.TryGetProperty("idPedido", out var el1) && el1.ValueKind == JsonValueKind.Number)
        {
            codigoPedido = el1.GetInt64();
        }
        else if (root.TryGetProperty("codigo_pedido_omie", out var el2) && el2.ValueKind == JsonValueKind.Number)
        {
            codigoPedido = el2.GetInt64();
        }

        if (codigoPedido == null || codigoPedido <= 0) return;

        // Extract numero pedido for notification
        string? numeroPedido = null;
        if (root.TryGetProperty("numeroPedido", out var np) && np.ValueKind == JsonValueKind.String)
        {
            numeroPedido = np.GetString();
        }
        else if (root.TryGetProperty("numero_pedido", out var np2) && np2.ValueKind == JsonValueKind.String)
        {
            numeroPedido = np2.GetString();
        }

        // Extract etapa
        string? etapa = null;
        if (root.TryGetProperty("etapa", out var et) && et.ValueKind == JsonValueKind.String)
        {
            etapa = et.GetString();
        }

        // Perform Business logic over the affected internal domain
        await pedidoSyncService.SyncByIdAsync(codigoPedido.Value);
        
        string title = webhookEvent.Event switch 
        {
            "VendaProduto.Incluida" => "Novo Pedido",
            "VendaProduto.Novo" => "Novo Pedido",
            "VendaProduto.Cancelada" => "Pedido Cancelado",
            "VendaProduto.Faturada" => "Pedido Faturado",
            _ => "Pedido Alterado"
        };
        
        var summary = $"Pedido {numeroPedido ?? codigoPedido.ToString()} - Etapa: {etapa ?? "N/A"} (Evento: {webhookEvent.Event})";
        
        foreach (var service in notificationServices)
        {
            await service.SendNotificationAsync(title, summary, "PEDIDO", codigoPedido.Value);
        }
    }
}
