using Domain.Interfaces;
using Domain.Interfaces.Transactions;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class WalletRepository : IWalletRepository
    {
        private readonly AppDbContext _context;

        public WalletRepository(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<Wallet> GetByIdAsync(Guid id)
        {
            return await _context.Wallets
                .Include(w => w.Transactions)
                .FirstOrDefaultAsync(w => w.Id == id);
        }

        public async Task<Wallet> GetBySellerIdAsync(Guid sellerId)
        {
            return await _context.Wallets
                .FirstOrDefaultAsync(w => w.SellerId == sellerId);
        }

        public async Task<Wallet> CreateAsync(Wallet wallet)
        {
            wallet.CreatedAt = DateTime.UtcNow;
            wallet.LastUpdateAt = DateTime.UtcNow;

            _context.Wallets.Add(wallet);
            await _context.SaveChangesAsync();

            return wallet;
        }

        public async Task<Wallet> UpdateAsync(Wallet wallet)
        {
            wallet.LastUpdateAt = DateTime.UtcNow;

            _context.Wallets.Update(wallet);
            await _context.SaveChangesAsync();

            return wallet;
        }

        public async Task<bool> ExistsAsync(Guid sellerId)
        {
            return await _context.Wallets.AnyAsync(w => w.SellerId == sellerId);
        }

        public async Task<IEnumerable<Wallet>> GetAllAsync(int page = 1, int pageSize = 10)
        {
            return await _context.Wallets
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IDbTransaction> BeginTransactionAsync()
        {
            return new EfDbTransaction(await _context.Database.BeginTransactionAsync());
        }
    }
}