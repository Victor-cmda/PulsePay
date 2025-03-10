using Application.DTOs;
using Shared.Enums;

namespace Application.Interfaces
{
    public interface IWalletTransactionService
    {
        Task<WalletTransactionDto> GetTransactionByIdAsync(Guid transactionId);
        Task<WalletTransactionDto> CreateTransactionAsync(Guid walletId, decimal amount, TransactionType type, string description, string reference = null);
        Task<WalletTransactionDto> ProcessTransactionAsync(Guid transactionId);
        Task<WalletTransactionDto> CancelTransactionAsync(Guid transactionId, string reason = null);
        Task<WalletBalanceDto> GetWalletBalanceAsync(Guid walletId);
        Task<List<WalletTransactionDto>> GetTransactionHistoryAsync(
            Guid walletId,
            DateTime? startDate = null,
            DateTime? endDate = null,
            TransactionType? type = null,
            TransactionStatus? status = null,
            int page = 1,
            int pageSize = 20);
        Task<IEnumerable<WalletTransactionDto>> GetAllPendingTransactionsAsync(int page = 1, int pageSize = 20);
        Task<int> GetTotalPendingTransactionsAsync();
    }
}