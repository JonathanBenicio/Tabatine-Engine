using Microsoft.Extensions.Logging;
using Tabatine.Core.Entities;

namespace Tabatine.Worker.Services.Handlers;

/// <summary>
/// Factory to provide the correct IWebhookEventHandler implementation for a given webhook event topic.
/// </summary>
public class WebhookHandlerFactory(IEnumerable<IWebhookEventHandler> handlers, ILogger<DefaultWebhookHandler> defaultLogger)
{
    // Default handler to cleanly ignore events without specific business logic
    private readonly DefaultWebhookHandler _defaultHandler = new(defaultLogger);

    /// <summary>
    /// Returns the mapped handler for the specific event, or the default handler if none match.
    /// </summary>
    public IWebhookEventHandler GetHandler(string eventName)
    {
        var handler = handlers.FirstOrDefault(h => h.SupportedEvents.Contains(eventName));
        return handler ?? _defaultHandler;
    }
}
