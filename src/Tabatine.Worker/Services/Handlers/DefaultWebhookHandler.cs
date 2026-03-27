using Microsoft.Extensions.Logging;
using Tabatine.Core.Entities;

namespace Tabatine.Worker.Services.Handlers;

/// <summary>
/// A fallback handler for events listed by Omie but not currently supported by our Sync Services. 
/// Safe-ignores the event, logging it so it doesn't stay blocked in the processing queue.
/// </summary>
public class DefaultWebhookHandler(ILogger<DefaultWebhookHandler> logger) : IWebhookEventHandler
{

    public IEnumerable<string> SupportedEvents => Enumerable.Empty<string>();

    public Task HandleAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken)
    {
        // We log the unhandled event generically
        // By returning normally, the event will be marked as "Processed" 
        // preventing infinite loops on unsupported features.
        logger.LogInformation("No specific handler configured for Omie Event '{Event}', ignoring payload.", webhookEvent.Event);
        
        return Task.CompletedTask;
    }
}
