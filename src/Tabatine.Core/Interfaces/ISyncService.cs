using System.Threading.Tasks;

namespace Tabatine.Core.Interfaces
{
    public interface ISyncService
    {
        Task SyncAllAsync(CancellationToken ct = default);
        Task SyncByIdAsync(long omieId, CancellationToken ct = default);
        Task CancelByIdAsync(long omieId, CancellationToken ct = default);
    }
}
