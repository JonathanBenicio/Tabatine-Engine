namespace Tabatine.Core.Entities;

public class SyncLock
{
    public string LockKey { get; set; } = null!;
    public string LockToken { get; set; } = null!;
    public DateTime AcquiredAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string? Owner { get; set; }
}
