using Domain.Models;

namespace Domain.Interfaces
{
    public interface IWalletRepository
    {
        Task<Wallet> GetByIdAsync(Guid id);
        Task<Wallet> GetBySellerIdAsync(Guid sellerId);
        Task<Wallet> CreateAsync(Wallet wallet);
        Task<Wallet> UpdateAsync(Wallet wallet);
        Task<bool> ExistsAsync(Guid sellerId);
        Task<IEnumerable<Wallet>> GetAllAsync(int page = 1, int pageSize = 10);
    }
}
