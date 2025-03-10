using Domain.Interfaces;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Enums;

namespace Infrastructure.Repositories
{
    public class CustomerPayoutRepository : ICustomerPayoutRepository
    {
        private readonly AppDbContext _context;

        public CustomerPayoutRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<CustomerPayout> CreateAsync(CustomerPayout payout)
        {
            _context.CustomerPayouts.Add(payout);
            await _context.SaveChangesAsync();
            return payout;
        }

        public async Task<CustomerPayout> UpdateAsync(CustomerPayout payout)
        {
            _context.CustomerPayouts.Update(payout);
            await _context.SaveChangesAsync();
            return payout;
        }

        public async Task<CustomerPayout> GetByIdAsync(Guid id)
        {
            return await _context.CustomerPayouts
                .Include(p => p.Transaction)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<CustomerPayout> GetByTransactionIdAsync(Guid transactionId)
        {
            return await _context.CustomerPayouts
                .FirstOrDefaultAsync(p => p.TransactionId == transactionId);
        }

        public async Task<IEnumerable<CustomerPayout>> GetBySellerIdAsync(Guid sellerId, int page = 1, int pageSize = 20)
        {
            return await _context.CustomerPayouts
                .Where(p => p.SellerId == sellerId)
                .OrderByDescending(p => p.RequestedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<CustomerPayout>> GetByStatusAsync(CustomerPayoutStatus status, int page = 1, int pageSize = 20)
        {
            return await _context.CustomerPayouts
                .Where(p => p.Status == status)
                .OrderByDescending(p => p.RequestedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetCountByStatusAsync(CustomerPayoutStatus status)
        {
            return await _context.CustomerPayouts
                .CountAsync(p => p.Status == status);
        }

        public async Task<decimal> GetTotalAmountByStatusAsync(CustomerPayoutStatus status)
        {
            return await _context.CustomerPayouts
                .Where(p => p.Status == status)
                .SumAsync(p => p.Amount);
        }
    }
}
