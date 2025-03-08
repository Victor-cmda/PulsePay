using Application.DTOs;
using Shared.Enums;

namespace Application.Interfaces
{
    public interface IWalletService
    {
        Task<WalletDto> GetWalletAsync(Guid id);
        Task<WalletDto> GetWalletByTypeAsync(Guid sellerId, WalletType walletType);
        Task<IEnumerable<WalletDto>> GetSellerWalletsAsync(Guid sellerId);
        Task<WalletWithTransactionsDto> GetWalletWithRecentTransactionsAsync(Guid walletId, int count = 10);
        Task<WalletDto> CreateWalletAsync(WalletCreateDto createDto);
        Task<WalletDto> UpdateBalanceAsync(Guid walletId, WalletUpdateDto updateDto);
        Task<WalletDto> SetDefaultWalletAsync(Guid walletId, Guid sellerId);
        Task<WalletDto> AddFundsAsync(Guid walletId, WalletOperationDto operationDto);
        Task<WalletDto> DeductFundsAsync(Guid walletId, WalletOperationDto operationDto);
        Task<List<WalletTransactionDto>> GetTransactionsAsync(Guid walletId, DateTime? startDate = null, DateTime? endDate = null, int page = 1, int pageSize = 20);
        Task<bool> HasSufficientFundsAsync(Guid walletId, decimal amount);
        Task<decimal> GetAvailableBalanceAsync(Guid walletId);
        Task<(WalletDto sourceWallet, WalletDto destinationWallet)> TransferBetweenWalletsAsync(
            Guid sourceWalletId,
            Guid destinationWalletId,
            decimal amount,
            string description = null);
    }
}