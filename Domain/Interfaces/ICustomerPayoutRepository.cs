using Domain.Models;
using Shared.Enums;

namespace Domain.Interfaces
{
    public interface ICustomerPayoutRepository
    {
        Task<CustomerPayout> CreateAsync(CustomerPayout payout);
        Task<CustomerPayout> UpdateAsync(CustomerPayout payout);
        Task<CustomerPayout> GetByIdAsync(Guid id);
        Task<CustomerPayout> GetByTransactionIdAsync(Guid transactionId);
        Task<IEnumerable<CustomerPayout>> GetBySellerIdAsync(Guid sellerId, int page = 1, int pageSize = 20);
        Task<IEnumerable<CustomerPayout>> GetByStatusAsync(CustomerPayoutStatus status, int page = 1, int pageSize = 20);
        Task<int> GetCountByStatusAsync(CustomerPayoutStatus status);
        Task<decimal> GetTotalAmountByStatusAsync(CustomerPayoutStatus status);
    }
}
