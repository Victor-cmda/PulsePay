using Domain.Interfaces;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class WithdrawRepository : IWithdrawRepository
    {
        private readonly AppDbContext _context;

        public WithdrawRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Withdraw> GetByIdAsync(Guid id)
        {
            return await _context.Withdraws
                .Include(w => w.BankAccount)
                .FirstOrDefaultAsync(w => w.Id == id);
        }

        public async Task<IEnumerable<Withdraw>> GetBySellerIdAsync(Guid sellerId, int page = 1, int pageSize = 10)
        {
            return await _context.Withdraws
                .Include(w => w.BankAccount)
                .Where(w => w.SellerId == sellerId)
                .OrderByDescending(w => w.RequestedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<Withdraw> CreateAsync(Withdraw withdraw)
        {
            withdraw.RequestedAt = DateTime.UtcNow;
            _context.Withdraws.Add(withdraw);
            await _context.SaveChangesAsync();
            return withdraw;
        }

        public async Task<Withdraw> UpdateAsync(Withdraw withdraw)
        {
            withdraw.ProcessedAt = DateTime.UtcNow;
            _context.Withdraws.Update(withdraw);
            await _context.SaveChangesAsync();
            return withdraw;
        }

        public async Task<IEnumerable<Withdraw>> GetPendingWithdrawsAsync()
        {
            return await _context.Withdraws
                .Include(w => w.BankAccount)
                .Where(w => w.Status == "Pending")
                .OrderBy(w => w.RequestedAt)
                .ToListAsync();
        }

        public async Task<decimal> GetTotalWithdrawnAmountAsync(Guid sellerId, DateTime startDate, DateTime endDate)
        {
            return await _context.Withdraws
                .Where(w => w.SellerId == sellerId &&
                           w.Status == "Completed" &&
                           w.RequestedAt >= startDate &&
                           w.RequestedAt <= endDate)
                .SumAsync(w => w.Amount);
        }
    }
}
