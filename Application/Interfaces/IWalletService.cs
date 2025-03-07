using Application.DTOs;

namespace Application.Interfaces
{
    public interface IWalletService
    {
        Task<WalletDto> GetWalletAsync(Guid sellerId);
        Task<WalletWithTransactionsDto> GetWalletWithRecentTransactionsAsync(Guid sellerId, int count = 10);
        Task<WalletDto> CreateWalletAsync(WalletCreateDto createDto);
        Task<WalletDto> UpdateBalanceAsync(Guid sellerId, WalletUpdateDto updateDto);
        Task<WalletDto> AddFundsAsync(Guid sellerId, WalletOperationDto operationDto);
        Task<WalletDto> DeductFundsAsync(Guid sellerId, WalletOperationDto operationDto);
        Task<List<WalletTransactionDto>> GetTransactionsAsync(Guid sellerId, DateTime? startDate = null, DateTime? endDate = null, int page = 1, int pageSize = 20);
        Task<bool> HasSufficientFundsAsync(Guid sellerId, decimal amount);
        Task<decimal> GetAvailableBalanceAsync(Guid sellerId);
    }
}