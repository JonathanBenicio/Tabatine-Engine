using System.Threading.Tasks;

namespace Tabatine.Core.Interfaces
{
    public interface INotificationService
    {
        Task SendNotificationAsync(string title, string message, string type, long? referenceId = null);
    }
}
