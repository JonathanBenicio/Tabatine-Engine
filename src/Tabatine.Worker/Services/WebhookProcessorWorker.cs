using Microsoft.EntityFrameworkCore;
using Tabatine.Infrastructure.Data;
using Tabatine.Worker.Services.Handlers;

namespace Tabatine.Worker.Services;

public partial class WebhookProcessorWorker(ILogger<WebhookProcessorWorker> logger, IServiceScopeFactory scopeFactory) : BackgroundService
{
    private const string StatusPending = "Pending";
    private const string StatusProcessed = "Processed";
    private const string StatusFailed = "Failed";

    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogWorkerIniciado(logger);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var processedAny = await ProcessNextMessageAsync(stoppingToken);

                // Se não processou nenhuma mensagem, aguarda para não sobrecarregar o banco
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
                LogErroCritico(logger, ex);
                await Task.Delay(_pollingInterval, stoppingToken);
            }
        }

        LogWorkerFinalizado(logger);
    }

    private async Task<bool> ProcessNextMessageAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // 1. Fase de De-queue (Atômica e Rápida)
        // Buscamos a mensagem e marcamos como 'Processing' imediatamente para liberar o banco.
        var strategy = dbContext.Database.CreateExecutionStrategy();
        var webhookEvent = await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var sql = $"SELECT * FROM webhook_events WHERE status = '{StatusPending}' ORDER BY created_at ASC LIMIT 1 FOR UPDATE SKIP LOCKED";
                var ev = await dbContext.WebhookEvents
                    .FromSqlRaw(sql)
                    .FirstOrDefaultAsync(cancellationToken);

                if (ev == null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return null;
                }

                ev.Status = "Processing";
                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                
                return ev;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });

        if (webhookEvent == null) return false;

        // 2. Fase de Processamento (Fora da transação da fila)
        LogProcessandoWebhook(logger, webhookEvent.Id, webhookEvent.Event);

        try
        {
            var handlerFactory = scope.ServiceProvider.GetRequiredService<WebhookHandlerFactory>();
            var handler = handlerFactory.GetHandler(webhookEvent.Event);
            
            // Aqui o handler pode rodar por muito tempo (Sync total) sem travar o banco
            await handler.HandleAsync(webhookEvent, cancellationToken);

            webhookEvent.Status = StatusProcessed;
            webhookEvent.ProcessedAt = DateTime.UtcNow;
            LogWebhookProcessado(logger, webhookEvent.Id, webhookEvent.Event);
        }
        catch (Exception ex)
        {
            webhookEvent.Status = StatusFailed;
            webhookEvent.ErrorMessage = ex.Message;
            webhookEvent.ProcessedAt = DateTime.UtcNow;
            LogErroProcessandoWebhook(logger, webhookEvent.Id, ex);
        }

        // 3. Atualização Final
        // Usamos a strategy novamente para garantir resiliência no update final
        await strategy.ExecuteAsync(async () => {
            dbContext.Update(webhookEvent);
            await dbContext.SaveChangesAsync(cancellationToken);
        });

        return true;
    }

    #region High-Performance Logging (LoggerMessage source generators)

    [LoggerMessage(Level = LogLevel.Information, Message = "WebhookProcessorWorker iniciado.")]
    private static partial void LogWorkerIniciado(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "WebhookProcessorWorker finalizado.")]
    private static partial void LogWorkerFinalizado(ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "Erro crítico no loop principal do WebhookProcessorWorker.")]
    private static partial void LogErroCritico(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Processando Webhook ID: {WebhookId} - Evento: {WebhookEvent}")]
    private static partial void LogProcessandoWebhook(ILogger logger, Guid webhookId, string webhookEvent);

    [LoggerMessage(Level = LogLevel.Information, Message = "Webhook ID: {WebhookId} ({WebhookEvent}) processado com sucesso.")]
    private static partial void LogWebhookProcessado(ILogger logger, Guid webhookId, string webhookEvent);

    [LoggerMessage(Level = LogLevel.Error, Message = "Erro processando Webhook ID: {WebhookId}")]
    private static partial void LogErroProcessandoWebhook(ILogger logger, Guid webhookId, Exception ex);

    #endregion
}
