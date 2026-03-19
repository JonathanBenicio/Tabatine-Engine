namespace Tabatine.Core.Entities;

public class SyncLock
{
    public string LockKey { get; set; } = null!;
    public string LockToken { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
}
