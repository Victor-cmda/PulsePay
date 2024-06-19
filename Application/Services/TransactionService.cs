using Application.Interfaces;
using Domain.Interfaces;
using Domain.Models;

namespace Application.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly ITransactionRepository _transactionRepository;

        public TransactionService(ITransactionRepository transactionRepository)
        {
            _transactionRepository = transactionRepository;
        }

        public async Task<Transaction> CreateTransactionAsync(Transaction transaction)
        {
            return await _transactionRepository.AddAsync(transaction);
        }

        public async Task<Transaction> UpdateTransactionAsync(Transaction transaction)
        {
            return await _transactionRepository.UpdateAsync(transaction);
        }

        public async Task<Transaction> GetTransactionByIdAsync(Guid id)
        {
            return await _transactionRepository.GetByIdAsync(id);
        }
    }
}
