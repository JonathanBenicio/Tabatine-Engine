using Microsoft.EntityFrameworkCore;
using Tabatine.Infrastructure.Data;
using Tabatine.Worker.Services.Handlers;

namespace Tabatine.Worker.Services;

public partial class WebhookProcessorWorker(
    ILogger<WebhookProcessorWorker> logger, 
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration) : BackgroundService
{
    private const string StatusPending = "Pending";
    private const string StatusProcessed = "Completed"; // De acordo com a spec
    private const string StatusFailed = "Failed";
    private const string StatusDeadLetter = "DeadLetter";

    private readonly TimeSpan _pollingInterval = TimeSpan.FromMilliseconds(
        configuration.GetValue<int>("WebhookProcessor:PollingIntervalMs", 5000));

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
        var skipLocked = configuration.GetValue<bool>("WebhookProcessor:UseSkipLocked", true);
        var skipLockedSql = skipLocked ? "FOR UPDATE SKIP LOCKED" : "FOR UPDATE";

        var webhookEvent = await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var sql = $@"
                    SELECT * FROM webhook_events 
                    WHERE status = '{StatusPending}' 
                       OR (status = '{StatusFailed}' AND next_retry_at <= NOW())
                    ORDER BY created_at ASC 
                    LIMIT 1 
                    {skipLockedSql}";
                
                var ev = await dbContext.WebhookEvents
                    .FromSqlRaw(sql)
                    .FirstOrDefaultAsync(cancellationToken);

                if (ev == null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    logger.LogTrace("Nenhuma mensagem disponível para processamento.");
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
            webhookEvent.LastAttemptAt = DateTime.UtcNow;
            LogWebhookProcessado(logger, webhookEvent.Id, webhookEvent.Event);
        }
        catch (Exception ex)
        {
            webhookEvent.RetryCount++;
            webhookEvent.LastErrorDetail = ex.ToString(); // Stacktrace completo para o Admin
            webhookEvent.LastAttemptAt = DateTime.UtcNow;

            if (webhookEvent.RetryCount >= webhookEvent.MaxRetries)
            {
                webhookEvent.Status = StatusDeadLetter;
                webhookEvent.NextRetryAt = null;
                LogErroProcessandoWebhook(logger, webhookEvent.Id, ex);
            }
            else
            {
                webhookEvent.Status = StatusFailed;
                // Backoff Exponencial: 2, 4, 8, 16, 32 minutos
                var delayMinutes = Math.Pow(2, webhookEvent.RetryCount);
                webhookEvent.NextRetryAt = DateTime.UtcNow.AddMinutes(delayMinutes);
                LogWebhookFalhouTentativa(logger, webhookEvent.Id, webhookEvent.RetryCount, webhookEvent.NextRetryAt.Value);
            }
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

    [LoggerMessage(Level = LogLevel.Warning, Message = "Webhook ID: {WebhookId} falhou. Tentativa {RetryCount}. Próxima tentativa em: {NextRetryAt}")]
    private static partial void LogWebhookFalhouTentativa(ILogger logger, Guid webhookId, int retryCount, DateTime nextRetryAt);

    #endregion
}
