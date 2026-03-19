using Microsoft.EntityFrameworkCore;
using Tabatine.Infrastructure.Data;
using Tabatine.Core.Interfaces;
using Tabatine.Core.Entities;

namespace Tabatine.Infrastructure.Services;

public class DbDistributedLockService : IDistributedLockService
{
    private readonly AppDbContext _db;

    public DbDistributedLockService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<bool> TryAcquireLockAsync(string key, string token, TimeSpan expiry, CancellationToken ct = default)
    {
        // Limpeza de locks expirados
        var expiredLocks = _db.SyncLocks.Where(l => l.ExpiresAt < DateTime.UtcNow);
        if (await expiredLocks.AnyAsync(ct))
        {
            _db.SyncLocks.RemoveRange(expiredLocks);
            await _db.SaveChangesAsync(ct);
        }

        try
        {
            var expiresAt = DateTime.UtcNow.Add(expiry);
            var newLock = new SyncLock 
            { 
                LockKey = key, 
                LockToken = token, 
                ExpiresAt = expiresAt 
            };
            _db.SyncLocks.Add(newLock);
            await _db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException)
        {
            // Trata conflito de PK (lock já existente)
            return false;
        }
    }

    public async Task ReleaseLockAsync(string key, string token, CancellationToken ct = default)
    {
        var @lock = await _db.SyncLocks
            .FirstOrDefaultAsync(l => l.LockKey == key && l.LockToken == token, ct);
        
        if (@lock != null)
        {
            _db.SyncLocks.Remove(@lock);
            await _db.SaveChangesAsync(ct);
        }
    }
}
