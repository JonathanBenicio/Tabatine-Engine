using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Tabatine.Core.Entities;
using Tabatine.Core.Interfaces;
using Tabatine.Infrastructure.Services;

namespace Tabatine.Worker.Services.Handlers;

/// <summary>
/// Webhook handler for Produto events.
/// Resolves event strings provided by Omie into actions over ProdutoSyncService and EstoqueSyncService.
/// </summary>
public class ProdutoWebhookHandler(
    ProdutoSyncService produtoSyncService, 
    EstoqueSyncService estoqueSyncService,
    IMemoryCache cache,
    IEnumerable<INotificationService> notificationServices,
    ILogger<ProdutoWebhookHandler> logger) : IWebhookEventHandler
{
    private static readonly TimeSpan DebounceOverride = TimeSpan.FromSeconds(10);

    public IEnumerable<string> SupportedEvents => new[]
    {
        "Produto.Incluido",
        "Produto.Alterado",
        "Produto.Excluido",
        "Produto.MovimentacaoEstoque",
        "Produto.AjusteEstoque"
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
        else if (root.TryGetProperty("id_produto", out var el2) && el2.ValueKind == JsonValueKind.Number)
        {
            codigoProduto = el2.GetInt64();
        }
        else if (root.TryGetProperty("nCodProd", out var el3) && el3.ValueKind == JsonValueKind.Number)
        {
            codigoProduto = el3.GetInt64();
        }

        if (codigoProduto == null || codigoProduto <= 0)
        {
            return;
        }

        bool isStockEvent = webhookEvent.Event == "Produto.MovimentacaoEstoque" || 
                           webhookEvent.Event == "Produto.AjusteEstoque";

        if (isStockEvent)
        {
            // Debouncing logic for stock balance updates
            var cacheKey = $"debounce_stock_sync_{codigoProduto.Value}";
            if (cache.TryGetValue(cacheKey, out _))
            {
                logger.LogDebug("Debouncing stock sync for Produto OmieId {OmieId}. Skipping event {Event}.", codigoProduto.Value, webhookEvent.Event);
                return;
            }

            cache.Set(cacheKey, true, DebounceOverride);
            await estoqueSyncService.SyncByIdAsync(codigoProduto.Value, cancellationToken);
        }
        else
        {
            await produtoSyncService.SyncByIdAsync(codigoProduto.Value);
        }
        
        string title = webhookEvent.Event switch 
        {
            "Produto.Incluido" => "Novo Produto",
            "Produto.Excluido" => "Produto Excluído",
            "Produto.AjusteEstoque" => "Ajuste de Estoque Realizado",
            "Produto.MovimentacaoEstoque" => "Movimentação de Estoque",
            _ => "Produto Alterado"
        };
        
        var category = isStockEvent ? "ESTOQUE" : "PRODUTO";
        var summary = isStockEvent 
            ? $"Saldo de Estoque do Produto ID {codigoProduto.Value} atualizado."
            : $"Produto ID {codigoProduto.Value} atualizado (Evento: {webhookEvent.Event}).";
        
        foreach (var service in notificationServices)
        {
            await service.SendNotificationAsync(title, summary, category, codigoProduto.Value);
        }
    }
}
