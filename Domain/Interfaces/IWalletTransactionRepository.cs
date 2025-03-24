using Domain.Models;
using Shared.Enums;

namespace Domain.Interfaces
{
    public interface IWalletTransactionRepository
    {
        Task<WalletTransaction> GetByIdAsync(Guid id);
        Task<List<WalletTransaction>> GetByWalletIdAsync(Guid walletId, DateTime? startDate = null, DateTime? endDate = null, int page = 1, int pageSize = 20);
        Task<List<WalletTransaction>> GetRecentByWalletIdAsync(Guid walletId, int count = 10);
        Task<List<WalletTransaction>> GetByWalletIdAndStatusAsync(Guid walletId, TransactionStatus status, int page = 1, int pageSize = 20);
        Task<WalletTransaction> CreateAsync(WalletTransaction transaction);
        Task<WalletTransaction> UpdateAsync(WalletTransaction transaction);
        Task<decimal> GetWalletBalanceAsync(Guid walletId);
        Task<int> GetTransactionCountAsync(Guid walletId, DateTime? startDate = null, DateTime? endDate = null, TransactionType? type = null, TransactionStatus? status = null);
        Task<List<WalletTransaction>> GetAllPendingTransactionsAsync(int page = 1, int pageSize = 20);
        Task<int> GetPendingTransactionsCountAsync();
    }
}