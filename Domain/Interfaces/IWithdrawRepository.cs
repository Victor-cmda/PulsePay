using Domain.Models;
using Shared.Enums;

namespace Domain.Interfaces
{
    public interface IWithdrawRepository
    {
        Task<Withdraw> CreateAsync(Withdraw withdraw);
        Task<Withdraw> UpdateAsync(Withdraw withdraw);
        Task<Withdraw> GetByIdAsync(Guid id);
        Task<IEnumerable<Withdraw>> GetBySellerIdAsync(Guid sellerId, int page = 1, int pageSize = 20);
        Task<IEnumerable<Withdraw>> GetByStatusAsync(WithdrawStatus status, int page = 1, int pageSize = 20);
        Task<int> GetCountByStatusAsync(WithdrawStatus status);
        Task<decimal> GetTotalAmountByStatusAsync(WithdrawStatus status);
    }
}
