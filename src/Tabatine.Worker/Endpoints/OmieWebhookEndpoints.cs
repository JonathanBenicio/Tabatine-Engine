using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Tabatine.Core.Interfaces;
using Tabatine.Infrastructure.Services;
using Tabatine.Worker.Models;

namespace Tabatine.Worker.Endpoints;

public static class OmieWebhookEndpoints
{
    public static void MapOmieWebhookEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/webhook/omie", async (
            [FromBody] OmieWebhookRequest request,
            IServiceProvider serviceProvider,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("OmieWebhook");
            logger.LogInformation("Recebido Webhook Omie: Evento={Event}", request.Event);

            try
            {
                if (request.Message == null) return Results.Ok();

                using var scope = serviceProvider.CreateScope();
                var pedidoSync = scope.ServiceProvider.GetRequiredService<PedidoSyncService>();
                var nfSync = scope.ServiceProvider.GetRequiredService<NotaFiscalSyncService>();
                var notificationServices = scope.ServiceProvider.GetServices<INotificationService>();

                string? messageJson = request.Message.ToString();
                if (string.IsNullOrEmpty(messageJson)) return Results.Ok();

                if (request.Event == "VendaProduto.Novo" || request.Event == "VendaProduto.Alterado")
                {
                    var pedidoMsg = JsonSerializer.Deserialize<OmieWebhookPedidoMessage>(messageJson);
                    if (pedidoMsg != null)
                    {
                        await pedidoSync.SyncByIdAsync(pedidoMsg.CodigoPedido);
                        
                        var title = request.Event == "VendaProduto.Novo" ? "Novo Pedido" : "Pedido Alterado";
                        var summary = $"Pedido {pedidoMsg.NumeroPedido} - Etapa: {pedidoMsg.Etapa}";
                        
                        foreach (var service in notificationServices)
                        {
                            await service.SendNotificationAsync(title, summary, "PEDIDO", pedidoMsg.CodigoPedido);
                        }
                    }
                }
                else if (request.Event == "Faturamento.NotaFiscalEmitida")
                {
                    var nfMsg = JsonSerializer.Deserialize<OmieWebhookNfMessage>(messageJson);
                    if (nfMsg != null)
                    {
                        await nfSync.SyncByIdAsync(nfMsg.CodigoNf);
                        
                        var title = "NF Emitida";
                        var summary = $"Nota Fiscal {nfMsg.NumeroNf} emitida.";
                        
                        foreach (var service in notificationServices)
                        {
                            await service.SendNotificationAsync(title, summary, "NF", nfMsg.CodigoNf);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao processar webhook Omie");
            }

            return Results.Ok();
        });
    }
}
