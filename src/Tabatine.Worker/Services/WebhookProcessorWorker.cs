using Microsoft.EntityFrameworkCore;
using Tabatine.Infrastructure.Data;
using Tabatine.Worker.Services.Handlers;

namespace Tabatine.Worker.Services;

public partial class WebhookProcessorWorker(
    ILogger<WebhookProcessorWorker> logger, 
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration) : BackgroundService
{
    private readonly TimeSpan _pollingInterval = TimeSpan.FromMilliseconds(
        configuration.GetValue<int>("WebhookProcessor:PollingIntervalMs", 5000));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogWorkerIniciado(logger);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var (processedAny, hasMore) = await ProcessNextMessagesAsync(stoppingToken);

                // Se não processou nenhuma mensagem e não há mais mensagens, aguarda
                if (!processedAny && !hasMore)
                {
                    await Task.Delay(_pollingInterval, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Worker parado graciosamente
                break;
            }
            catch (ObjectDisposedException)
            {
                // Significa que o app está fechando e o provider foi descartado
                logger.LogWarning("WebhookProcessorWorker: Provider descartado. Encerrando worker.");
                break;
            }
            catch (Exception ex)
            {
                LogErroCritico(logger, ex);
                try 
                {
                    await Task.Delay(_pollingInterval, stoppingToken);
                }
                catch (OperationCanceledException) { break; }
            }
        }

        LogWorkerFinalizado(logger);
    }

    private async Task<(bool Processed, bool HasMore)> ProcessNextMessagesAsync(CancellationToken cancellationToken)
    {
        IServiceScope? scope = null;
        try
        {
            scope = scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var strategy = dbContext.Database.CreateExecutionStrategy();

            // 1. Fase de De-queue (Atômica e Rápida)
            var skipLocked = configuration.GetValue<bool>("WebhookProcessor:UseSkipLocked", true);
            var skipLockedSql = skipLocked ? "FOR UPDATE SKIP LOCKED" : "FOR UPDATE";

            WebhookEvent? webhookEvent = await strategy.ExecuteAsync(async () =>
            {
                if (cancellationToken.IsCancellationRequested) return null;

                await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    var sql = $@"
                        SELECT * FROM webhook_events 
                        WHERE status = '{WebhookEvent.StatusPending}' 
                           OR (status = '{WebhookEvent.StatusFailed}' AND next_retry_at <= NOW() + INTERVAL '1 second')
                        ORDER BY created_at ASC 
                        LIMIT 1 
                        {skipLockedSql}";
                    
                    var ev = await dbContext.WebhookEvents
                        .FromSqlRaw(sql)
                        .AsNoTracking()
                        .FirstOrDefaultAsync(cancellationToken);

                    if (ev == null)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        return null;
                    }

                    logger.LogDebug("Webhook encontrado: ID={Id}, Evento={Event}, Status={Status}", ev.Id, ev.Event, ev.Status);

                    ev.Status = WebhookEvent.StatusProcessing;
                    
                    await dbContext.Database.ExecuteSqlInterpolatedAsync(
                        $"UPDATE webhook_events SET status = {WebhookEvent.StatusProcessing} WHERE id = {ev.Id}", 
                        cancellationToken);

                    await transaction.CommitAsync(cancellationToken);
                    return ev;
                }
                catch
                {
                    try { if (dbContext.Database.CurrentTransaction != null) await transaction.RollbackAsync(cancellationToken); } catch { }
                    throw;
                }
            });

            if (webhookEvent == null) return (false, false);

            // 2. Fase de Processamento (Fora da transação da fila)
            LogProcessandoWebhook(logger, webhookEvent.Id, webhookEvent.Event);

            try
            {
                var handlerFactory = scope.ServiceProvider.GetRequiredService<WebhookHandlerFactory>();
                var handler = handlerFactory.GetHandler(webhookEvent.Event);
                await handler.HandleAsync(webhookEvent, cancellationToken);

                webhookEvent.Status = WebhookEvent.StatusCompleted;
                webhookEvent.ProcessedAt = DateTime.UtcNow;
                webhookEvent.LastAttemptAt = DateTime.UtcNow;
                LogWebhookProcessado(logger, webhookEvent.Id, webhookEvent.Event);
            }
            catch (Exception ex)
            {
                webhookEvent.RetryCount++;
                webhookEvent.LastErrorDetail = ex.ToString();
                webhookEvent.LastAttemptAt = DateTime.UtcNow;

                if (webhookEvent.RetryCount >= webhookEvent.MaxRetries)
                {
                    webhookEvent.Status = WebhookEvent.StatusDeadLetter;
                    webhookEvent.NextRetryAt = null;
                    LogErroProcessandoWebhook(logger, webhookEvent.Id, ex);
                }
                else
                {
                    webhookEvent.Status = WebhookEvent.StatusFailed;
                    var delayMinutes = Math.Pow(2, webhookEvent.RetryCount);
                    webhookEvent.NextRetryAt = DateTime.UtcNow.AddMinutes(delayMinutes);
                    LogWebhookFalhouTentativa(logger, webhookEvent.Id, webhookEvent.RetryCount, webhookEvent.NextRetryAt.Value);
                }
            }

            // 3. Atualização Final
            await strategy.ExecuteAsync(async () => {
                if (cancellationToken.IsCancellationRequested) return;

                var dbEvent = await dbContext.WebhookEvents.FirstOrDefaultAsync(w => w.Id == webhookEvent.Id, cancellationToken);
                if (dbEvent != null)
                {
                    dbEvent.Status = webhookEvent.Status;
                    dbEvent.ProcessedAt = webhookEvent.ProcessedAt;
                    dbEvent.LastAttemptAt = webhookEvent.LastAttemptAt;
                    dbEvent.LastErrorDetail = webhookEvent.LastErrorDetail;
                    dbEvent.RetryCount = webhookEvent.RetryCount;
                    dbEvent.NextRetryAt = webhookEvent.NextRetryAt;
                    
                    await dbContext.SaveChangesAsync(cancellationToken);
                    logger.LogTrace("Webhook {Id} atualizado para status {Status}.", webhookEvent.Id, webhookEvent.Status);
                }
                else
                {
                    logger.LogError("Webhook {Id} não encontrado para atualização final.", webhookEvent.Id);
                }
            });

            // Verificar se há mais mensagens pendentes para notificar o loop principal
            var hasMore = await dbContext.WebhookEvents
                .AnyAsync(w => w.Status == WebhookEvent.StatusPending 
                            || (w.Status == WebhookEvent.StatusFailed && w.NextRetryAt <= DateTime.UtcNow), 
                          cancellationToken);

            return (true, hasMore);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Erro no loop de processamento de webhooks: {Message}", ex.Message);
            return (false, false);
        }
        catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
        {
            return (false, false);
        }
        finally
        {
            scope?.Dispose();
        }
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

    [LoggerMessage(Level = LogLevel.Warning, Message = "Webhook ID: {WebhookId} falhou. Tentativa {RetryCount}. Próxima tentativa em: {NextRetryAt}")]
    private static partial void LogWebhookFalhouTentativa(ILogger logger, Guid webhookId, int retryCount, DateTime nextRetryAt);

    #endregion
}
