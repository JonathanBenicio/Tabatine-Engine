using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tabatine.Core.Interfaces
{
    public interface ISyncStateRepository
    {
        Task<DateTime?> GetLastSyncDateAsync(string moduleName, CancellationToken ct = default);
        Task SetLastSyncDateAsync(string moduleName, DateTime syncDate, CancellationToken ct = default);
    }
}
