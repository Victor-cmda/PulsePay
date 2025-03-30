using Application.DTOs;
using Domain.Models;

namespace Application.Interfaces
{
    public interface ITransactionService
    {
        Task<Transaction> CreateTransactionAsync(Transaction transaction);
        Task<Transaction> UpdateTransactionAsync(Transaction transaction);
        Task<Transaction> GetTransactionByIdAsync(Guid id);
        Task SendTransactionNotificationAsync(Guid id, string status);
        Task<Transaction> UpdateTransactionStatusAsync(Guid id, string status);
    }
}
