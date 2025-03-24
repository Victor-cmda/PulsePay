using Application.DTOs;

namespace Application.Interfaces
{
    public interface IListenerService
    {
        Task<bool> GenerateNotification(NotificationDto notification);
    }
}
