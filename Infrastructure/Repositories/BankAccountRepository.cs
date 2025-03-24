using Domain.Interfaces;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class BankAccountRepository : IBankAccountRepository
    {
        private readonly AppDbContext _context;

        public BankAccountRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<BankAccount> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.BankAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        }

        public async Task<IEnumerable<BankAccount>> GetBySellerIdAsync(Guid sellerId, CancellationToken cancellationToken = default)
        {
            return await _context.BankAccounts
                .AsNoTracking()
                .Where(b => b.SellerId == sellerId)
                .OrderBy(b => b.BankName)
                .ThenBy(b => b.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<BankAccount> CreateAsync(BankAccount bankAccount, CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            bankAccount.CreatedAt = now;
            bankAccount.LastUpdatedAt = now;

            _context.BankAccounts.Add(bankAccount);
            await _context.SaveChangesAsync(cancellationToken);
            return bankAccount;
        }

        public async Task<BankAccount> UpdateAsync(BankAccount bankAccount, CancellationToken cancellationToken = default)
        {
            bankAccount.LastUpdatedAt = DateTime.UtcNow;

            _context.Entry(bankAccount).State = EntityState.Modified;
            await _context.SaveChangesAsync(cancellationToken);
            return bankAccount;
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var deletedRows = await _context.BankAccounts
                .Where(b => b.Id == id)
                .ExecuteDeleteAsync(cancellationToken);

            return deletedRows > 0;
        }

        public async Task<bool> ExistsByAccountNumberAsync(string bankCode, string accountNumber, string branchNumber, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(bankCode) || string.IsNullOrEmpty(accountNumber) || string.IsNullOrEmpty(branchNumber))
                return false;

            return await _context.BankAccounts
                .AsNoTracking()
                .AnyAsync(b =>
                    b.BankCode == bankCode &&
                    b.AccountNumber == accountNumber &&
                    b.BranchNumber == branchNumber,
                    cancellationToken);
        }

        public async Task<bool> IsOwnerAsync(Guid id, Guid sellerId, CancellationToken cancellationToken = default)
        {
            return await _context.BankAccounts
                .AsNoTracking()
                .AnyAsync(b =>
                    b.Id == id && b.SellerId == sellerId,
                    cancellationToken);
        }

        public async Task<bool> ExistsByPixKeyAsync(string pixKey, PixKeyType pixKeyType, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(pixKey))
                return false;

            return await _context.BankAccounts
                .AsNoTracking()
                .AnyAsync(b =>
                    b.PixKey == pixKey &&
                    b.PixKeyType == pixKeyType &&
                    b.AccountType == BankAccountType.PIX,
                    cancellationToken);
        }

        public async Task<IEnumerable<BankAccount>> GetUnverifiedAccountsAsync(int page = 1, int pageSize = 20)
        {
            return await _context.BankAccounts
                .Where(b => !b.IsVerified)
                .OrderBy(b => b.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetTotalCountAsync()
        {
            return await _context.BankAccounts.CountAsync();
        }
    }
}