using System.Text.Json;
using Tabatine.Core.Entities;
using Tabatine.Core.Interfaces;
using Tabatine.Infrastructure.Services;

namespace Tabatine.Worker.Services.Handlers;

/// <summary>
/// Webhook handler for Produto events.
/// Resolves event strings provided by Omie into actions over ProdutoSyncService.
/// </summary>
public class ProdutoWebhookHandler(ProdutoSyncService produtoSyncService, IEnumerable<INotificationService> notificationServices) : IWebhookEventHandler
{

    public IEnumerable<string> SupportedEvents => new[]
    {
        "Produto.Incluido",
        "Produto.Alterado",
        "Produto.Excluido",
        "Produto.MovimentacaoEstoque",
        "Produto.AjusteEstoque"
        // Let's not include the marketplace or pdv specific events unless requested, 
        // to show we map specifically only what we wish.
    };

    public async Task HandleAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(webhookEvent.Payload)) return;

        using var doc = JsonDocument.Parse(webhookEvent.Payload);
        var root = doc.RootElement;
        
        long? codigoProduto = null;
        if (root.TryGetProperty("idProduto", out var el0) && el0.ValueKind == JsonValueKind.Number)
        {
            codigoProduto = el0.GetInt64();
        }
        else if (root.TryGetProperty("codigo_produto", out var el1) && el1.ValueKind == JsonValueKind.Number)
        {
            codigoProduto = el1.GetInt64();
        }

        // Often Webhooks from omie just send the code directly as id_produto
        if (codigoProduto == null && root.TryGetProperty("id_produto", out var el2) && el2.ValueKind == JsonValueKind.Number)
        {
            codigoProduto = el2.GetInt64();
        }

        if (codigoProduto == null || codigoProduto <= 0)
        {
            return; // Needs an ID to sync
        }

        await produtoSyncService.SyncByIdAsync(codigoProduto.Value);
        
        string title = webhookEvent.Event switch 
        {
            "Produto.Incluido" => "Novo Produto",
            "Produto.Excluido" => "Produto Excluído",
            "Produto.AjusteEstoque" => "Ajuste de Estoque Realizado",
            _ => "Produto Alterado"
        };
        
        var summary = $"Produto ID {codigoProduto.Value} atualizado (Evento: {webhookEvent.Event}).";
        
        foreach (var service in notificationServices)
        {
            await service.SendNotificationAsync(title, summary, "PRODUTO", codigoProduto.Value);
        }
    }
}
