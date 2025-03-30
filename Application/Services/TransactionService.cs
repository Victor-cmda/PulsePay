using Application.Interfaces;
using Domain.Interfaces;
using Domain.Models;
using Microsoft.Extensions.Logging;

namespace Application.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly ITransactionRepository _transactionRepository;
        private readonly IListenerService _listenerService;
        private readonly ILogger<TransactionService> _logger;
        public TransactionService(
            ITransactionRepository transactionRepository,
            IListenerService listenerService,
            ILogger<TransactionService> logger)
        {
            _transactionRepository = transactionRepository;
            _listenerService = listenerService;
            _logger = logger;
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

        public async Task SendTransactionNotificationAsync(Guid id, string status)
        {
            var transaction = await GetTransactionByIdAsync(id);
            if (transaction == null)
            {
                _logger.LogWarning("Cannot send notification for non-existent transaction {TransactionId}", id);
                return;
            }

            try
            {
                var notification = NotificationHelpers.CreatePaymentNotification(transaction, status);

                await _listenerService.GenerateNotification(notification);

                _logger.LogInformation("Transaction notification sent: {TransactionId}, Status: {Status}",
                    transaction.Id, status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending transaction notification {TransactionId}", id);
            }
        }

        public async Task<Transaction> UpdateTransactionStatusAsync(Guid id, string status)
        {
            var transaction = await GetTransactionByIdAsync(id);
            if (transaction == null)
            {
                throw new Exception($"Transaction with ID {id} not found");
            }

            transaction.Status = status;

            if (status == "APPROVED" || status == "COMPLETED" || status == "PAID")
            {
                transaction.PaidAt = DateTime.UtcNow;
            }

            var updated = await _transactionRepository.UpdateAsync(transaction);

            await SendTransactionNotificationAsync(id, status);

            return updated;
        }
    }
}
