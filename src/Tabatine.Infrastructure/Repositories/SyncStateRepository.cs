using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tabatine.Core.Entities;
using Tabatine.Core.Interfaces;
using Tabatine.Infrastructure.Data;

namespace Tabatine.Infrastructure.Repositories
{
    public class SyncStateRepository : ISyncStateRepository
    {
        private readonly AppDbContext _dbContext;

        public SyncStateRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<DateTime?> GetLastSyncDateAsync(string moduleName, CancellationToken ct = default)
        {
            var state = await _dbContext.Set<IntegrationSyncState>()
                .FirstOrDefaultAsync(s => s.ModuleName == moduleName, ct);
            
            return state?.LastSyncDate;
        }

        public async Task SetLastSyncDateAsync(string moduleName, DateTime syncDate, CancellationToken ct = default)
        {
            var state = await _dbContext.Set<IntegrationSyncState>()
                .FirstOrDefaultAsync(s => s.ModuleName == moduleName, ct);

            if (state == null)
            {
                state = new IntegrationSyncState
                {
                    Id = Guid.NewGuid(),
                    ModuleName = moduleName,
                    LastSyncDate = syncDate,
                    UpdatedAt = DateTime.UtcNow
                };
                _dbContext.Set<IntegrationSyncState>().Add(state);
            }
            else
            {
                state.LastSyncDate = syncDate;
                state.UpdatedAt = DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync(ct);
        }
    }
}
