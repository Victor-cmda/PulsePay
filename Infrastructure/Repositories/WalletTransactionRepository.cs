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
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<WalletTransaction> GetByIdAsync(Guid id)
        {
            return await _context.WalletTransactions
                .Include(wt => wt.Wallet)
                .FirstOrDefaultAsync(wt => wt.Id == id);
        }

        public async Task<List<WalletTransaction>> GetByWalletIdAsync(
            Guid walletId,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int page = 1,
            int pageSize = 20)
        {
            var query = _context.WalletTransactions
                .Where(wt => wt.WalletId == walletId);

            if (startDate.HasValue)
            {
                query = query.Where(wt => wt.CreatedAt >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(wt => wt.CreatedAt <= endDate.Value);
            }

            return await query
                .OrderByDescending(wt => wt.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<WalletTransaction>> GetRecentByWalletIdAsync(Guid walletId, int count = 10)
        {
            return await _context.WalletTransactions
                .Where(wt => wt.WalletId == walletId)
                .OrderByDescending(wt => wt.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<List<WalletTransaction>> GetByWalletIdAndStatusAsync(
            Guid walletId,
            TransactionStatus status,
            int page = 1,
            int pageSize = 20)
        {
            return await _context.WalletTransactions
                .Where(wt => wt.WalletId == walletId && wt.Status == status)
                .OrderByDescending(wt => wt.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<WalletTransaction> CreateAsync(WalletTransaction transaction)
        {
            transaction.CreatedAt = DateTime.UtcNow;

            if (transaction.Status == TransactionStatus.Completed && !transaction.ProcessedAt.HasValue)
            {
                transaction.ProcessedAt = DateTime.UtcNow;
            }

            _context.WalletTransactions.Add(transaction);
            await _context.SaveChangesAsync();

            return transaction;
        }

        public async Task<WalletTransaction> UpdateAsync(WalletTransaction transaction)
        {
            // Certifica que ProcessedAt é preenchido para transações completadas
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
                if (transaction.Type == TransactionType.Credit ||
                    transaction.Type == TransactionType.Deposit ||
                    transaction.Type == TransactionType.Refund)
                {
                    balance += transaction.Amount;
                }
                else if (transaction.Type == TransactionType.Debit ||
                         transaction.Type == TransactionType.Withdraw)
                {
                    balance -= transaction.Amount;
                }
            }

            return balance;
        }

        public async Task<int> GetTransactionCountAsync(
            Guid walletId,
            DateTime? startDate = null,
            DateTime? endDate = null,
            TransactionType? type = null,
            TransactionStatus? status = null)
        {
            var query = _context.WalletTransactions
                .Where(wt => wt.WalletId == walletId);

            if (startDate.HasValue)
            {
                query = query.Where(wt => wt.CreatedAt >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(wt => wt.CreatedAt <= endDate.Value);
            }

            if (type.HasValue)
            {
                query = query.Where(wt => wt.Type == type.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(wt => wt.Status == status.Value);
            }

            return await query.CountAsync();
        }

        public async Task<List<WalletTransaction>> GetAllPendingTransactionsAsync(int page = 1, int pageSize = 20)
        {
            return await _context.WalletTransactions
                .Where(t => t.Status == TransactionStatus.Pending)
                .OrderByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetPendingTransactionsCountAsync()
        {
            return await _context.WalletTransactions
                .CountAsync(t => t.Status == TransactionStatus.Pending);
        }
    }
}