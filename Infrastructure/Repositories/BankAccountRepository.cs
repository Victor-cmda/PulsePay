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

        public async Task<BankAccount> GetByIdAsync(Guid id)
        {
            return await _context.BankAccounts
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<IEnumerable<BankAccount>> GetBySellerIdAsync(Guid sellerId)
        {
            return await _context.BankAccounts
                .Where(b => b.SellerId == sellerId)
                .OrderBy(b => b.BankName)
                .ToListAsync();
        }

        public async Task<BankAccount> CreateAsync(BankAccount bankAccount)
        {
            bankAccount.CreatedAt = DateTime.UtcNow;
            bankAccount.LastUpdatedAt = DateTime.UtcNow;

            _context.BankAccounts.Add(bankAccount);
            await _context.SaveChangesAsync();

            return bankAccount;
        }

        public async Task<BankAccount> UpdateAsync(BankAccount bankAccount)
        {
            bankAccount.LastUpdatedAt = DateTime.UtcNow;

            _context.BankAccounts.Update(bankAccount);
            await _context.SaveChangesAsync();

            return bankAccount;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var bankAccount = await _context.BankAccounts.FindAsync(id);
            if (bankAccount == null)
                return false;

            _context.BankAccounts.Remove(bankAccount);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> ExistsByAccountNumberAsync(string bankCode, string accountNumber, string branchNumber)
        {
            return await _context.BankAccounts.AnyAsync(b =>
                b.BankCode == bankCode &&
                b.AccountNumber == accountNumber &&
                b.BranchNumber == branchNumber);
        }

        public async Task<bool> IsOwnerAsync(Guid id, Guid sellerId)
        {
            return await _context.BankAccounts.AnyAsync(b =>
                b.Id == id && b.SellerId == sellerId);
        }

        public async Task<bool> ExistsByPixKeyAsync(string pixKey, PixKeyType pixKeyType)
        {
            if (string.IsNullOrEmpty(pixKey))
                return false;

            return await _context.BankAccounts
                .AnyAsync(b =>
                    b.PixKey == pixKey &&
                    b.PixKeyType == pixKeyType &&
                    b.AccountType == BankAccountType.PIX);
        }
    }
}
