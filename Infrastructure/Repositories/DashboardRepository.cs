using Domain.Interfaces;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class TransactionRepository : ITransactionRepository
    {
        private readonly AppDbContext _context;

        public TransactionRepository(AppDbContext context)
        {
            _context = context;
        }
        public async Task<Transaction> AddAsync(Transaction transaction)
        {
            try
            {
                await _context.Set<Transaction>().AddAsync(transaction);
                await _context.SaveChangesAsync();
                return transaction;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error saving transaction: {ex.InnerException?.Message}", ex);
            }
        }

        public async Task<Transaction> UpdateAsync(Transaction transaction)
        {
            _context.Set<Transaction>().Update(transaction);
            await _context.SaveChangesAsync();
            return transaction;
        }

        public async Task<Transaction> GetByIdAsync(Guid Id)
        {
            return await _context.Set<Transaction>().FindAsync(Id);
        }
    }
}
