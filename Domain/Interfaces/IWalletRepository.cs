using Domain.Interfaces.Transactions;
using Domain.Models;
using Shared.Enums;

namespace Domain.Interfaces
{
    public interface IWalletRepository
    {
        Task<Wallet> GetByIdAsync(Guid id);
        Task<Wallet> GetBySellerIdAndTypeAsync(Guid sellerId, WalletType walletType);
        Task<IEnumerable<Wallet>> GetAllBySellerIdAsync(Guid sellerId);
        Task<Wallet> CreateAsync(Wallet wallet);
        Task<Wallet> UpdateAsync(Wallet wallet);
        Task<int> CountBySellerIdAsync(Guid sellerId);
        Task<bool> ExistsAsync(Guid sellerId, WalletType walletType);
        Task<IEnumerable<Wallet>> GetAllAsync(int page = 1, int pageSize = 10);
        Task<IDbTransaction> BeginTransactionAsync();
    }
}