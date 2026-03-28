using Microsoft.EntityFrameworkCore;
using Tabatine.Infrastructure.Data;

namespace Tabatine.Worker.Services;

public partial class WebhookProcessorWorker(ILogger<WebhookProcessorWorker> logger, IServiceScopeFactory scopeFactory) : BackgroundService
{
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

        // Wrapping na ExecutionStrategy para compatibilidade com NpgsqlRetryingExecutionStrategy
        var strategy = dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
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

                LogProcessandoWebhook(logger, webhookEvent.Id, webhookEvent.Event);

                try
                {
                    var handlerFactory = scope.ServiceProvider.GetRequiredService<Tabatine.Worker.Services.Handlers.WebhookHandlerFactory>();
                    var handler = handlerFactory.GetHandler(webhookEvent.Event);
                    await handler.HandleAsync(webhookEvent, cancellationToken);

                    // Sucesso
                    webhookEvent.Status = "Processed";
                    webhookEvent.ProcessedAt = DateTime.UtcNow;
                    LogWebhookProcessado(logger, webhookEvent.Id);
                }
                catch (Exception ex)
                {
                    // Falha no processamento da regra de negócio (Dead Letter Queue marker)
                    webhookEvent.Status = "Failed";
                    webhookEvent.ErrorMessage = ex.Message;
                    webhookEvent.ProcessedAt = DateTime.UtcNow;
                    LogErroProcessandoWebhook(logger, webhookEvent.Id, ex);
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
        });
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

    [LoggerMessage(Level = LogLevel.Information, Message = "Webhook ID: {WebhookId} processado com sucesso.")]
    private static partial void LogWebhookProcessado(ILogger logger, Guid webhookId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Erro processando Webhook ID: {WebhookId}")]
    private static partial void LogErroProcessandoWebhook(ILogger logger, Guid webhookId, Exception ex);

    #endregion
}
