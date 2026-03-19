namespace Tabatine.Core.Interfaces;

public interface IDistributedLockService
{
    Task<bool> TryAcquireLockAsync(string key, string token, TimeSpan expiry, CancellationToken ct = default);
    Task ReleaseLockAsync(string key, string token, CancellationToken ct = default);
}
