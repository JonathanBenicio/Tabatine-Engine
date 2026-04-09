using Microsoft.EntityFrameworkCore;
using Tabatine.Infrastructure.Data;
using Tabatine.Core.Interfaces;
using Tabatine.Core.Entities;

namespace Tabatine.Infrastructure.Services;

// Usa IDbContextFactory para criar uma conexão independente a cada operação.
// Isso é obrigatório para evitar que o lock participe de transações externas.
public class DbDistributedLockService(IDbContextFactory<AppDbContext> dbFactory) : IDistributedLockService
{
    public async Task<bool> TryAcquireLockAsync(string key, string token, TimeSpan expiry, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var now = DateTime.UtcNow;
        var expiresAt = now.Add(expiry);

        // Atômico: Limpa o lock apenas se ele já existir e estiver expirado.
        await db.SyncLocks
            .Where(l => l.LockKey == key && l.ExpiresAt <= now)
            .ExecuteDeleteAsync(ct);

        // Tenta inserir o novo lock com ON CONFLICT.
        // O Postgres retornará 1 se inserido, 0 se houve conflito (lock ainda válido).
        // Isso evita o ruído de DbUpdateException nos logs.
        var affectedRows = await db.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO sync_locks (lock_key, lock_token, expires_at) VALUES ({key}, {token}, {expiresAt}) ON CONFLICT (lock_key) DO NOTHING", 
            ct);

        return affectedRows > 0;
    }

    public async Task ReleaseLockAsync(string key, string token, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        
        // Atômico: Remove apenas se a chave E o token coincidirem.
        // O ExecuteDeleteAsync é ideal aqui porque não carrega a entidade para memória.
        await db.SyncLocks
            .Where(l => l.LockKey == key && l.LockToken == token)
            .ExecuteDeleteAsync(ct);
    }
}
