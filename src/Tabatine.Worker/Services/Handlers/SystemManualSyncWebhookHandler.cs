using Microsoft.Extensions.Logging;
using Tabatine.Core.Entities;
using Tabatine.Core.Interfaces;
using Tabatine.Worker.Services.Handlers;

namespace Tabatine.Worker.Services.Handlers;

public class SystemManualSyncWebhookHandler(ISyncService syncService, ILogger<SystemManualSyncWebhookHandler> logger) : IWebhookEventHandler
{
    public IEnumerable<string> SupportedEvents => new[] { "System.ManualSync" };

    public async Task HandleAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken)
    {
        logger.LogInformation("Iniciando Sincronização Manual disparada pela fila (ID: {WebhookId}).", webhookEvent.Id);

        // Execute full sync for all configured modules
        await syncService.SyncAllAsync(cancellationToken);

        logger.LogInformation("Sincronização Manual (ID: {WebhookId}) concluída com sucesso.", webhookEvent.Id);
    }
}
