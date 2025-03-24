using Domain.Models;

namespace Domain.Interfaces
{
    public interface ITransactionRepository
    {
        Task<Transaction> AddAsync(Transaction transaction);
        Task<Transaction> UpdateAsync(Transaction transaction);
        Task<Transaction> GetByIdAsync(Guid Id);
        Task<Transaction> GetByPaymentIdAsync(string paymentId);
    }
}
