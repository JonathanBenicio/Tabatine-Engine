namespace Tabatine.Core.Entities;

public class WebhookEvent
{
    public const string StatusPending = "Pending";
    public const string StatusProcessing = "Processing";
    public const string StatusCompleted = "Completed";
    public const string StatusFailed = "Failed";
    public const string StatusDeadLetter = "DeadLetter";

    public Guid Id { get; set; } = Guid.NewGuid();

    public string AppKey { get; set; } = string.Empty;

    public string Event { get; set; } = string.Empty;

    public string Payload { get; set; } = string.Empty;

    public string Status { get; set; } = StatusPending;

    public string? LastErrorDetail { get; set; }

    /// <summary>
    /// Omie Connect 2.0 messageId for idempotent deduplication.
    /// </summary>
    public string? MessageId { get; set; }

    public int RetryCount { get; set; }
    
    public int MaxRetries { get; set; } = 5;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ProcessedAt { get; set; }
    
    public DateTime? LastAttemptAt { get; set; }
    
    public DateTime? NextRetryAt { get; set; }
}

