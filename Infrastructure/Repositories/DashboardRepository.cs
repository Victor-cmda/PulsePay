using Domain.Interfaces;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly AppDbContext _context;

        public DashboardRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Transaction>> GetTransactionsBySellers(List<Guid> sellers)
        {
            return await _context.Transactions.Where(t => sellers.Contains(t.SellerId)).OrderBy(x=>x.CreatedAt).ToListAsync();
        }
    }
}
