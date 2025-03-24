using Domain.Interfaces;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Enums;

namespace Infrastructure.Repositories
{
    public class WithdrawRepository : IWithdrawRepository
    {
        private readonly AppDbContext _context;

        public WithdrawRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Withdraw> CreateAsync(Withdraw withdraw)
        {
            _context.Withdraws.Add(withdraw);
            await _context.SaveChangesAsync();
            return withdraw;
        }

        public async Task<Withdraw> UpdateAsync(Withdraw withdraw)
        {
            _context.Withdraws.Update(withdraw);
            await _context.SaveChangesAsync();
            return withdraw;
        }

        public async Task<Withdraw> GetByIdAsync(Guid id)
        {
            return await _context.Withdraws
                .Include(w => w.BankAccount)
                .FirstOrDefaultAsync(w => w.Id == id);
        }

        public async Task<IEnumerable<Withdraw>> GetBySellerIdAsync(Guid sellerId, int page = 1, int pageSize = 20)
        {
            return await _context.Withdraws
                .Include(w => w.BankAccount)
                .Where(w => w.SellerId == sellerId)
                .OrderByDescending(w => w.RequestedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<Withdraw>> GetByStatusAsync(WithdrawStatus status, int page = 1, int pageSize = 20)
        {
            return await _context.Withdraws
                .Include(w => w.BankAccount)
                .Where(w => w.Status == status)
                .OrderByDescending(w => w.RequestedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetCountByStatusAsync(WithdrawStatus status)
        {
            return await _context.Withdraws
                .CountAsync(w => w.Status == status);
        }

        public async Task<decimal> GetTotalAmountByStatusAsync(WithdrawStatus status)
        {
            return await _context.Withdraws
                .Where(w => w.Status == status)
                .SumAsync(w => w.Amount);
        }
    }
}
