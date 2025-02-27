using Domain.Interfaces;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Enums;

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
                .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        }

        public async Task<IEnumerable<BankAccount>> GetBySellerIdAsync(Guid sellerId, CancellationToken cancellationToken = default)
        {
            return await _context.BankAccounts
                .Where(b => b.SellerId == sellerId)
                .OrderBy(b => b.BankName)
                .ToListAsync(cancellationToken);
        }

        public async Task<BankAccount> CreateAsync(BankAccount bankAccount, CancellationToken cancellationToken = default)
        {
            bankAccount.CreatedAt = DateTime.UtcNow;
            bankAccount.LastUpdatedAt = DateTime.UtcNow;
            _context.BankAccounts.Add(bankAccount);
            await _context.SaveChangesAsync(cancellationToken);
            return bankAccount;
        }

        public async Task<BankAccount> UpdateAsync(BankAccount bankAccount, CancellationToken cancellationToken = default)
        {
            bankAccount.LastUpdatedAt = DateTime.UtcNow;
            _context.BankAccounts.Update(bankAccount);
            await _context.SaveChangesAsync(cancellationToken);
            return bankAccount;
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var bankAccount = await _context.BankAccounts.FindAsync(new object[] { id }, cancellationToken);
            if (bankAccount == null)
                return false;

            _context.BankAccounts.Remove(bankAccount);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<bool> ExistsByAccountNumberAsync(string bankCode, string accountNumber, string branchNumber, CancellationToken cancellationToken = default)
        {
            return await _context.BankAccounts.AnyAsync(b =>
                b.BankCode == bankCode &&
                b.AccountNumber == accountNumber &&
                b.BranchNumber == branchNumber,
                cancellationToken);
        }

        public async Task<bool> IsOwnerAsync(Guid id, Guid sellerId, CancellationToken cancellationToken = default)
        {
            return await _context.BankAccounts.AnyAsync(b =>
                b.Id == id && b.SellerId == sellerId,
                cancellationToken);
        }

        public async Task<bool> ExistsByPixKeyAsync(string pixKey, PixKeyType pixKeyType, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(pixKey))
                return false;

            return await _context.BankAccounts
                .AnyAsync(b =>
                    b.PixKey == pixKey &&
                    b.PixKeyType == pixKeyType &&
                    b.AccountType == BankAccountType.PIX,
                    cancellationToken);
        }
    }
}