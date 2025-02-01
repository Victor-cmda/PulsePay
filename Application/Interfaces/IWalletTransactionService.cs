using Domain.Models;
using Shared.Enums;

namespace Application.Interfaces
{
    public interface IWalletTransactionService
    {
        Task<WalletTransaction> CreateTransactionAsync(Guid walletId, decimal amount, TransactionType type, string description, string? reference = null);
        Task<WalletTransaction> ProcessTransactionAsync(Guid transactionId);
        Task<WalletTransaction> CancelTransactionAsync(Guid transactionId, string reason);
        Task<decimal> GetWalletBalanceAsync(Guid walletId);
        Task<IEnumerable<WalletTransaction>> GetTransactionHistoryAsync(Guid walletId, DateTime startDate, DateTime endDate);
        Task<bool> HasSufficientFundsAsync(Guid walletId, decimal amount);
        Task<WalletTransaction> GetTransactionByIdAsync(Guid transactionId);
    }
}
