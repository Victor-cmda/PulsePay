using Domain.Models;

namespace Domain.Interfaces
{
    public interface IWithdrawRepository
    {
        Task<Withdraw> GetByIdAsync(Guid id);
        Task<IEnumerable<Withdraw>> GetBySellerIdAsync(Guid sellerId, int page = 1, int pageSize = 10);
        Task<Withdraw> CreateAsync(Withdraw withdraw);
        Task<Withdraw> UpdateAsync(Withdraw withdraw);
        Task<IEnumerable<Withdraw>> GetPendingWithdrawsAsync();
        Task<decimal> GetTotalWithdrawnAmountAsync(Guid sellerId, DateTime startDate, DateTime endDate);
    }
}
