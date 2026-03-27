using System.Text.Json;
using Tabatine.Core.Entities;
using Tabatine.Core.Interfaces;
using Tabatine.Infrastructure.Services;
using Tabatine.Worker.Models;

namespace Tabatine.Worker.Services.Handlers;

/// <summary>
/// Webhook handler for VendaProduto events.
/// Resolves event strings provided by Omie into actions over PedidoSyncService.
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

        var pedidoMsg = JsonSerializer.Deserialize<OmieWebhookPedidoMessage>(webhookEvent.Payload);
        if (pedidoMsg == null) return;

        // Perform Business logic over the affected internal domain
        await pedidoSyncService.SyncByIdAsync(pedidoMsg.CodigoPedido);
        
        string title = webhookEvent.Event switch 
        {
            "VendaProduto.Incluida" => "Novo Pedido",
            "VendaProduto.Novo" => "Novo Pedido",
            "VendaProduto.Cancelada" => "Pedido Cancelado",
            "VendaProduto.Faturada" => "Pedido Faturado",
            _ => "Pedido Alterado"
        };
        
        var summary = $"Pedido {pedidoMsg.NumeroPedido} - Etapa: {pedidoMsg.Etapa} (Evento: {webhookEvent.Event})";
        
        foreach (var service in notificationServices)
        {
            await service.SendNotificationAsync(title, summary, "PEDIDO", pedidoMsg.CodigoPedido);
        }
    }
}
