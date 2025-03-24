using Domain.Models;
using Shared.Enums;

namespace Domain.Interfaces
{
    public interface IDepositRepository
    {
        Task<Deposit> CreateAsync(Deposit deposit);
        Task<Deposit> UpdateAsync(Deposit deposit);
        Task<Deposit> GetByIdAsync(Guid id);
        Task<Deposit> GetByTransactionIdAsync(string transactionId);
        Task<IEnumerable<Deposit>> GetBySellerIdAsync(Guid sellerId, int page = 1, int pageSize = 20);
        Task<IEnumerable<Deposit>> GetByStatusAsync(DepositStatus status, int page = 1, int pageSize = 20);
    }
}
