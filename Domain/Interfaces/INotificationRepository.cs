using Domain.Models;

namespace Domain.Interfaces
{
    public interface INotificationRepository
    {
        Task<Notification> AddAsync(Notification notification);
        Task<Notification> UpdateAsync(Notification notification);
        Task<Notification> GetByIdAsync(Guid Id);
        Task<List<Notification>> GetPendingNotificationsAsync();
    }
}
