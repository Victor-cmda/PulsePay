using Domain.Models;
using Shared.Enums;

namespace Domain.Interfaces
{
    public interface IBankAccountRepository
    {
        Task<BankAccount> GetByIdAsync(Guid id);
        Task<IEnumerable<BankAccount>> GetBySellerIdAsync(Guid sellerId);
        Task<BankAccount> CreateAsync(BankAccount bankAccount);
        Task<BankAccount> UpdateAsync(BankAccount bankAccount);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> ExistsByAccountNumberAsync(string bankCode, string accountNumber, string branchNumber);
        Task<bool> ExistsByPixKeyAsync(string pixKey, PixKeyType pixKeyType);
        Task<bool> IsOwnerAsync(Guid id, Guid sellerId);
    }
}
