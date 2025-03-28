using Domain.Models;
using Shared.Enums;

namespace Domain.Interfaces
{
    public interface IRefundRepository
    {
        Task<Refund> CreateAsync(Refund refund);
        Task<Refund> UpdateAsync(Refund refund);
        Task<Refund> GetByIdAsync(Guid id);
        Task<IEnumerable<Refund>> GetBySellerIdAsync(Guid sellerId, int page = 1, int pageSize = 20);
        Task<IEnumerable<Refund>> GetByStatusAsync(RefundStatus status, int page = 1, int pageSize = 20);
        Task<int> GetCountByStatusAsync(RefundStatus status);
    }
}