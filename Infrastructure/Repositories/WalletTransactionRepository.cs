using Domain.Interfaces;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Enums;

namespace Infrastructure.Repositories
{
    public class WalletTransactionRepository : IWalletTransactionRepository
    {
        private readonly AppDbContext _context;

        public WalletTransactionRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<WalletTransaction> GetByIdAsync(Guid id)
        {
            return await _context.WalletTransactions
                .Include(wt => wt.Wallet)
                .FirstOrDefaultAsync(wt => wt.Id == id);
        }

        public async Task<IEnumerable<WalletTransaction>> GetByWalletIdAsync(Guid walletId)
        {
            return await _context.WalletTransactions
                .Where(wt => wt.WalletId == walletId)
                .OrderByDescending(wt => wt.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<WalletTransaction>> GetByWalletIdAndStatusAsync(Guid walletId, TransactionStatus status)
        {
            return await _context.WalletTransactions
                .Where(wt => wt.WalletId == walletId && wt.Status == status)
                .OrderByDescending(wt => wt.CreatedAt)
                .ToListAsync();
        }

        public async Task<WalletTransaction> CreateAsync(WalletTransaction transaction)
        {
            transaction.CreatedAt = DateTime.UtcNow;

            if (transaction.Status == TransactionStatus.Completed)
            {
                transaction.ProcessedAt = DateTime.UtcNow;
            }

            _context.WalletTransactions.Add(transaction);
            await _context.SaveChangesAsync();

            return transaction;
        }

        public async Task<WalletTransaction> UpdateAsync(WalletTransaction transaction)
        {
            if (transaction.Status == TransactionStatus.Completed && !transaction.ProcessedAt.HasValue)
            {
                transaction.ProcessedAt = DateTime.UtcNow;
            }

            _context.WalletTransactions.Update(transaction);
            await _context.SaveChangesAsync();

            return transaction;
        }

        public async Task<decimal> GetWalletBalanceAsync(Guid walletId)
        {
            var completedTransactions = await _context.WalletTransactions
                .Where(wt => wt.WalletId == walletId && wt.Status == TransactionStatus.Completed)
                .ToListAsync();

            decimal balance = 0;
            foreach (var transaction in completedTransactions)
            {
                if (transaction.Type == TransactionType.Credit || transaction.Type == TransactionType.Refund)
                {
                    balance += transaction.Amount;
                }
                else if (transaction.Type == TransactionType.Debit || transaction.Type == TransactionType.Withdraw)
                {
                    balance -= transaction.Amount;
                }
            }

            return balance;
        }

        public async Task<IEnumerable<WalletTransaction>> GetTransactionHistoryAsync(Guid walletId, DateTime startDate, DateTime endDate)
        {
            return await _context.WalletTransactions
                .Where(wt => wt.WalletId == walletId &&
                            wt.CreatedAt >= startDate &&
                            wt.CreatedAt <= endDate)
                .OrderByDescending(wt => wt.CreatedAt)
                .ToListAsync();
        }
    }
}
