using Domain.Models;
using Shared.Enums;

namespace Domain.Interfaces
{
    public interface IBankAccountRepository
    {
        Task<BankAccount> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IEnumerable<BankAccount>> GetBySellerIdAsync(Guid sellerId, CancellationToken cancellationToken = default);
        Task<BankAccount> CreateAsync(BankAccount bankAccount, CancellationToken cancellationToken = default);
        Task<BankAccount> UpdateAsync(BankAccount bankAccount, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
        Task<bool> ExistsByAccountNumberAsync(string bankCode, string accountNumber, string branchNumber, CancellationToken cancellationToken = default);
        Task<bool> ExistsByPixKeyAsync(string pixKey, PixKeyType pixKeyType, CancellationToken cancellationToken = default);
        Task<bool> IsOwnerAsync(Guid id, Guid sellerId, CancellationToken cancellationToken = default);
        Task<IEnumerable<BankAccount>> GetUnverifiedAccountsAsync(int page = 1, int pageSize = 20);
        Task<int> GetTotalCountAsync();
    }
}