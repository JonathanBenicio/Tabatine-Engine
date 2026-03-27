using Tabatine.Core.Entities;

namespace Tabatine.Worker.Services.Handlers;

/// <summary>
/// Interface for handling specific webhook events from Omie.
/// Implementations of this interface process events matching the SupportedEvents list.
/// </summary>
public interface IWebhookEventHandler
{
    /// <summary>
    /// The list of Omie webhook topics/events that this handler supports.
    /// E.g. "VendaProduto.Novo", "VendaProduto.Alterada", "ClienteFornecedor.Incluido".
    /// </summary>
    IEnumerable<string> SupportedEvents { get; }

    /// <summary>
    /// Processes the webhook event asynchronously.
    /// </summary>
    /// <param name="webhookEvent">The generic webhook event retrieved from the database queue.</param>
    /// <param name="cancellationToken">Cancellation token for aborting the operation.</param>
    /// <returns>A task representing the asynchronous processing operation.</returns>
    Task HandleAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken);
}
