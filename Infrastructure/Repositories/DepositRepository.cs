using Domain.Interfaces;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Enums;

namespace Infrastructure.Repositories
{
    public class DepositRepository : IDepositRepository
    {
        private readonly AppDbContext _context;

        public DepositRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Deposit> CreateAsync(Deposit deposit)
        {
            _context.Deposits.Add(deposit);
            await _context.SaveChangesAsync();
            return deposit;
        }

        public async Task<Deposit> UpdateAsync(Deposit deposit)
        {
            _context.Deposits.Update(deposit);
            await _context.SaveChangesAsync();
            return deposit;
        }

        public async Task<Deposit> GetByIdAsync(Guid id)
        {
            return await _context.Deposits
                .FirstOrDefaultAsync(d => d.Id == id);
        }

        public async Task<Deposit> GetByTransactionIdAsync(string transactionId)
        {
            return await _context.Deposits
                .FirstOrDefaultAsync(d => d.TransactionId == transactionId);
        }

        public async Task<IEnumerable<Deposit>> GetBySellerIdAsync(Guid sellerId, int page = 1, int pageSize = 20)
        {
            return await _context.Deposits
                .Where(d => d.SellerId == sellerId)
                .OrderByDescending(d => d.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<Deposit>> GetByStatusAsync(DepositStatus status, int page = 1, int pageSize = 20)
        {
            return await _context.Deposits
                .Where(d => d.Status == status)
                .OrderByDescending(d => d.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
    }
}
