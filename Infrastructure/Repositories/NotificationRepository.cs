using Domain.Interfaces;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly AppDbContext _context;
        public NotificationRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Notification> AddAsync(Notification notification)
        {
            try
            {
                await _context.Set<Notification>().AddAsync(notification);
                await _context.SaveChangesAsync();
                return notification;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error saving notification: {ex.InnerException?.Message}", ex);
            }
        }

        public async Task<Notification> UpdateAsync(Notification notification)
        {
            _context.Set<Notification>().Update(notification);
            await _context.SaveChangesAsync();
            return notification;
        }

        public async Task<Notification> GetByIdAsync(Guid Id)
        {
            return await _context.Set<Notification>().FindAsync(Id);
        }

        public async Task<List<Notification>> GetPendingNotificationsAsync()
        {
            return await _context.Set<Notification>().Where(n => n.Status == "PENDING").ToListAsync();
        }
    }
}
