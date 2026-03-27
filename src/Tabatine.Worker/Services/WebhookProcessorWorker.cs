using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tabatine.Core.Entities;
using Tabatine.Core.Interfaces;
using Tabatine.Infrastructure.Data;
using Tabatine.Infrastructure.Services;
using Tabatine.Worker.Models;

namespace Tabatine.Worker.Services;

public class WebhookProcessorWorker : BackgroundService
{
    private readonly ILogger<WebhookProcessorWorker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(5);

    public WebhookProcessorWorker(ILogger<WebhookProcessorWorker> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("WebhookProcessorWorker iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var processedAny = await ProcessNextMessageAsync(stoppingToken);

                // Se não procressou nenhuma mensagem, aguarda para não sobrecarregar o banco
                if (!processedAny)
                {
                    await Task.Delay(_pollingInterval, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Worker parado
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro crítico no loop principal do WebhookProcessorWorker.");
                await Task.Delay(_pollingInterval, stoppingToken);
            }
        }

        _logger.LogInformation("WebhookProcessorWorker finalizado.");
    }

    private async Task<bool> ProcessNextMessageAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Usando transação explícita para o FOR UPDATE SKIP LOCKED
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // Busca a próxima mensagem pendente travando a linha (Skip Locked) para concorrência segura
            var sql = "SELECT * FROM \"WebhookEvents\" WHERE \"Status\" = 'Pending' ORDER BY \"CreatedAt\" ASC LIMIT 1 FOR UPDATE SKIP LOCKED";
            var webhookEvent = await dbContext.WebhookEvents
                .FromSqlRaw(sql)
                .FirstOrDefaultAsync(cancellationToken);

            if (webhookEvent == null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            _logger.LogInformation("Processando Webhook ID: {Id} - Evento: {Event}", webhookEvent.Id, webhookEvent.Event);

            try
            {
                var pedidoSync = scope.ServiceProvider.GetRequiredService<PedidoSyncService>();
                var nfSync = scope.ServiceProvider.GetRequiredService<NotaFiscalSyncService>();
                var notificationServices = scope.ServiceProvider.GetServices<INotificationService>();

                if (webhookEvent.Event == "VendaProduto.Novo" || webhookEvent.Event == "VendaProduto.Alterado")
                {
                    var pedidoMsg = JsonSerializer.Deserialize<OmieWebhookPedidoMessage>(webhookEvent.Payload);
                    if (pedidoMsg != null)
                    {
                        await pedidoSync.SyncByIdAsync(pedidoMsg.CodigoPedido);
                        
                        var title = webhookEvent.Event == "VendaProduto.Novo" ? "Novo Pedido" : "Pedido Alterado";
                        var summary = $"Pedido {pedidoMsg.NumeroPedido} - Etapa: {pedidoMsg.Etapa}";
                        
                        foreach (var service in notificationServices)
                        {
                            await service.SendNotificationAsync(title, summary, "PEDIDO", pedidoMsg.CodigoPedido);
                        }
                    }
                }
                else if (webhookEvent.Event == "Faturamento.NotaFiscalEmitida")
                {
                    var nfMsg = JsonSerializer.Deserialize<OmieWebhookNfMessage>(webhookEvent.Payload);
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

                // Sucesso
                webhookEvent.Status = "Processed";
                webhookEvent.ProcessedAt = DateTime.UtcNow;
                _logger.LogInformation("Webhook ID: {Id} processado com sucesso.", webhookEvent.Id);
            }
            catch (Exception ex)
            {
                // Falha no processamento da regra de negócio (Dead Letter Queue marker)
                webhookEvent.Status = "Failed";
                webhookEvent.ErrorMessage = ex.Message;
                webhookEvent.ProcessedAt = DateTime.UtcNow;
                _logger.LogError(ex, "Erro processando Webhook ID: {Id}", webhookEvent.Id);
            }

            // Atualizar e comitar transação
            dbContext.Update(webhookEvent);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return true;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
