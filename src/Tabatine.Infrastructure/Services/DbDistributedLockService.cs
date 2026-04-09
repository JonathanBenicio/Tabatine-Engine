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

        try
        {
            // Atômico: Limpa o lock apenas se ele já existir e estiver expirado.
            // Isso evita que tentemos adicionar um lock que ainda é válido.
            await db.SyncLocks
                .Where(l => l.LockKey == key && l.ExpiresAt <= now)
                .ExecuteDeleteAsync(ct);

            // Tenta inserir o novo lock.
            // Se o ExecuteDelete não removeu nada (porque o lock ainda é válido), 
            // esta inserção falhará por violação de Chave Primária (PK).
            var newLock = new SyncLock 
            { 
                LockKey = key, 
                LockToken = token, 
                ExpiresAt = now.Add(expiry)
            };

            db.SyncLocks.Add(newLock);
            await db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException)
        {
            // Outro worker já possui um lock válido ou ganhou a corrida de inserção.
            return false;
        }
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
