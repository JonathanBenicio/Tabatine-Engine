using System;
using System.Threading.Tasks;
using Tabatine.Core.Entities;
using Tabatine.Core.Interfaces;
using Tabatine.Infrastructure.Data;

namespace Tabatine.Infrastructure.Services
{
    public class SupabaseNotificationService : INotificationService
    {
        private readonly AppDbContext _dbContext;

        public SupabaseNotificationService(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task SendNotificationAsync(string title, string message, string type, long? referenceId = null)
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                Title = title,
                Message = message,
                Type = type,
                ReferenceId = referenceId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Notifications.Add(notification);
            await _dbContext.SaveChangesAsync();
        }
    }
}
