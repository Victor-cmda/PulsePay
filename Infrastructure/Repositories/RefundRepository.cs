using Domain.Interfaces;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class RefundRepository : IRefundRepository
    {
        private readonly AppDbContext _context;

        public RefundRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Refund> CreateAsync(Refund refund)
        {
            _context.Set<Refund>().Add(refund);
            await _context.SaveChangesAsync();
            return refund;
        }

        public async Task<Refund> UpdateAsync(Refund refund)
        {
            _context.Set<Refund>().Update(refund);
            await _context.SaveChangesAsync();
            return refund;
        }

        public async Task<Refund> GetByIdAsync(Guid id)
        {
            return await _context.Set<Refund>()
                .Include(r => r.Transaction)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<IEnumerable<Refund>> GetBySellerIdAsync(Guid sellerId, int page = 1, int pageSize = 20)
        {
            return await _context.Set<Refund>()
                .Where(r => r.SellerId == sellerId)
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<Refund>> GetByStatusAsync(RefundStatus status, int page = 1, int pageSize = 20)
        {
            return await _context.Set<Refund>()
                .Where(r => r.Status == status)
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetCountByStatusAsync(RefundStatus status)
        {
            return await _context.Set<Refund>()
                .CountAsync(r => r.Status == status);
        }
    }
}