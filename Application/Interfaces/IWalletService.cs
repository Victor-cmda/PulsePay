using Application.DTOs;

namespace Application.Interfaces
{
    public interface IWalletService
    {
        Task<WalletResponseDto> GetWalletAsync(Guid sellerId);
        Task<WalletResponseDto> CreateWalletAsync(WalletCreateDto createDto);
        Task<WalletResponseDto> UpdateBalanceAsync(Guid sellerId, WalletUpdateDto updateDto);
        Task<WalletResponseDto> AddFundsAsync(Guid sellerId, decimal amount);
        Task<WalletResponseDto> DeductFundsAsync(Guid sellerId, decimal amount);
        Task<bool> HasSufficientFundsAsync(Guid sellerId, decimal amount);
        Task<decimal> GetAvailableBalanceAsync(Guid sellerId);
    }
}
