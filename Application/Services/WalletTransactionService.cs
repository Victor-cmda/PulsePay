using Application.Interfaces;
using Domain.Interfaces;
using Domain.Models;
using Microsoft.Extensions.Logging;
using Shared.Enums;
using Shared.Exceptions;
using System.ComponentModel.DataAnnotations;
using ValidationException = Shared.Exceptions.ValidationException;

namespace Application.Services
{
    public class WalletTransactionService : IWalletTransactionService
    {
        private readonly IWalletTransactionRepository _transactionRepository;
        private readonly IWalletRepository _walletRepository;
        private readonly ILogger<WalletTransactionService> _logger;

        public WalletTransactionService(
            IWalletTransactionRepository transactionRepository,
            IWalletRepository walletRepository,
            ILogger<WalletTransactionService> logger)
        {
            _transactionRepository = transactionRepository;
            _walletRepository = walletRepository;
            _logger = logger;
        }

        public async Task<WalletTransaction> CreateTransactionAsync(
            Guid walletId,
            decimal amount,
            TransactionType type,
            string description,
            string? reference = null)
        {
            var wallet = await _walletRepository.GetByIdAsync(walletId);
            if (wallet == null)
            {
                throw new NotFoundException("Carteira não encontrada");
            }

            if (amount <= 0)
            {
                throw new ValidationException("O valor da transação deve ser maior que zero");
            }

            // Verifica saldo disponível para débitos
            if ((type == TransactionType.Debit || type == TransactionType.Withdraw)
                && !await HasSufficientFundsAsync(walletId, amount))
            {
                throw new InsufficientFundsException("Saldo insuficiente para realizar a operação");
            }

            var transaction = new WalletTransaction
            {
                Id = Guid.NewGuid(),
                WalletId = walletId,
                Amount = amount,
                Type = type,
                Status = TransactionStatus.Pending,
                Description = description,
                Reference = reference,
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                var result = await _transactionRepository.CreateAsync(transaction);
                _logger.LogInformation($"Transação criada: {result.Id} - Tipo: {type} - Valor: {amount}");

                // Processa imediatamente se for crédito
                if (type == TransactionType.Credit)
                {
                    return await ProcessTransactionAsync(result.Id);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao criar transação para wallet {walletId}");
                throw;
            }
        }

        public async Task<WalletTransaction> ProcessTransactionAsync(Guid transactionId)
        {
            var transaction = await _transactionRepository.GetByIdAsync(transactionId);
            if (transaction == null)
            {
                throw new NotFoundException("Transação não encontrada");
            }

            if (transaction.Status != TransactionStatus.Pending)
            {
                throw new ValidationException("Apenas transações pendentes podem ser processadas");
            }

            // Verifica novamente o saldo para débitos no momento do processamento
            if ((transaction.Type == TransactionType.Debit || transaction.Type == TransactionType.Withdraw)
                && !await HasSufficientFundsAsync(transaction.WalletId, transaction.Amount))
            {
                transaction.Status = TransactionStatus.Failed;
                await _transactionRepository.UpdateAsync(transaction);
                throw new InsufficientFundsException("Saldo insuficiente para processar a transação");
            }

            try
            {
                transaction.Status = TransactionStatus.Completed;
                transaction.ProcessedAt = DateTime.UtcNow;

                var result = await _transactionRepository.UpdateAsync(transaction);
                _logger.LogInformation($"Transação processada: {transactionId}");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao processar transação {transactionId}");
                throw;
            }
        }

        public async Task<WalletTransaction> CancelTransactionAsync(Guid transactionId, string reason)
        {
            var transaction = await _transactionRepository.GetByIdAsync(transactionId);
            if (transaction == null)
            {
                throw new NotFoundException("Transação não encontrada");
            }

            if (transaction.Status != TransactionStatus.Pending)
            {
                throw new ValidationException("Apenas transações pendentes podem ser canceladas");
            }

            try
            {
                transaction.Status = TransactionStatus.Cancelled;
                transaction.Description += $" | Cancelada: {reason}";

                var result = await _transactionRepository.UpdateAsync(transaction);
                _logger.LogInformation($"Transação cancelada: {transactionId} - Motivo: {reason}");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao cancelar transação {transactionId}");
                throw;
            }
        }

        public async Task<decimal> GetWalletBalanceAsync(Guid walletId)
        {
            try
            {
                return await _transactionRepository.GetWalletBalanceAsync(walletId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao obter saldo da wallet {walletId}");
                throw;
            }
        }

        public async Task<IEnumerable<WalletTransaction>> GetTransactionHistoryAsync(
            Guid walletId,
            DateTime startDate,
            DateTime endDate)
        {
            try
            {
                return await _transactionRepository.GetTransactionHistoryAsync(walletId, startDate, endDate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao obter histórico de transações para wallet {walletId}");
                throw;
            }
        }

        public async Task<bool> HasSufficientFundsAsync(Guid walletId, decimal amount)
        {
            var balance = await GetWalletBalanceAsync(walletId);
            return balance >= amount;
        }

        public async Task<WalletTransaction> GetTransactionByIdAsync(Guid transactionId)
        {
            var transaction = await _transactionRepository.GetByIdAsync(transactionId);
            if (transaction == null)
            {
                throw new NotFoundException("Transação não encontrada");
            }
            return transaction;
        }
    }
}
