using Domain.Models;
using Shared.Enums;

namespace Domain.Interfaces
{
    public interface IWalletTransactionRepository
    {
        Task<WalletTransaction> GetByIdAsync(Guid id);
        Task<IEnumerable<WalletTransaction>> GetByWalletIdAsync(Guid walletId);
        Task<IEnumerable<WalletTransaction>> GetByWalletIdAndStatusAsync(Guid walletId, TransactionStatus status);
        Task<WalletTransaction> CreateAsync(WalletTransaction transaction);
        Task<WalletTransaction> UpdateAsync(WalletTransaction transaction);
        Task<decimal> GetWalletBalanceAsync(Guid walletId);
        Task<IEnumerable<WalletTransaction>> GetTransactionHistoryAsync(Guid walletId, DateTime startDate, DateTime endDate);
    }
}
