using Application.DTOs;
using Application.Interfaces;
using Domain.Interfaces;
using Domain.Models;
using Microsoft.Extensions.Logging;
using Shared.Enums;
using Shared.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
            _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
            _walletRepository = walletRepository ?? throw new ArgumentNullException(nameof(walletRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<WalletTransactionDto> CreateTransactionAsync(
    Guid walletId,
    decimal amount,
    TransactionType type,
    string description,
    string reference = null)
        {
            var wallet = await _walletRepository.GetByIdAsync(walletId);
            if (wallet == null)
            {
                throw new NotFoundException($"Carteira com ID {walletId} não encontrada");
            }

            if (amount <= 0)
            {
                throw new ValidationException("O valor da transação deve ser maior que zero");
            }

            // Enforce wallet type restrictions
            switch (type)
            {
                case TransactionType.Credit:
                case TransactionType.Deposit:
                case TransactionType.Refund when wallet.WalletType == WalletType.Withdrawal:
                    // These are incoming transactions - only allowed for Deposit or General wallets
                    if (wallet.WalletType == WalletType.Withdrawal)
                    {
                        throw new ValidationException($"Transações do tipo {type} não são permitidas em carteiras de Saque (Withdrawal). Use uma carteira de Depósito (Deposit) ou Geral (General).");
                    }
                    break;

                case TransactionType.Debit:
                case TransactionType.Withdraw:
                case TransactionType.Refund when wallet.WalletType == WalletType.Deposit:
                    // These are outgoing transactions - only allowed for Withdrawal or General wallets
                    if (wallet.WalletType == WalletType.Deposit)
                    {
                        throw new ValidationException($"Transações do tipo {type} não são permitidas em carteiras de Depósito (Deposit). Use uma carteira de Saque (Withdrawal) ou Geral (General).");
                    }
                    break;
            }

            // Verify balance for outgoing transactions
            if ((type == TransactionType.Debit || type == TransactionType.Withdraw)
                && wallet.AvailableBalance < amount)
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
                await using var dbTransaction = await _walletRepository.BeginTransactionAsync();

                try
                {
                    // Create the transaction
                    var result = await _transactionRepository.CreateAsync(transaction);

                    // Update the wallet balance
                    if (type == TransactionType.Credit || type == TransactionType.Deposit || type == TransactionType.Refund)
                    {
                        // Incoming transactions
                        wallet.AvailableBalance += amount;
                        wallet.TotalBalance = wallet.AvailableBalance + wallet.PendingBalance;
                        transaction.Status = TransactionStatus.Completed;
                        transaction.ProcessedAt = DateTime.UtcNow;
                        await _transactionRepository.UpdateAsync(transaction);
                    }
                    else if (type == TransactionType.Debit || type == TransactionType.Withdraw)
                    {
                        // Outgoing transactions
                        wallet.AvailableBalance -= amount;
                        wallet.TotalBalance = wallet.AvailableBalance + wallet.PendingBalance;
                        transaction.Status = TransactionStatus.Completed;
                        transaction.ProcessedAt = DateTime.UtcNow;
                        await _transactionRepository.UpdateAsync(transaction);
                    }

                    wallet.LastUpdateAt = DateTime.UtcNow;
                    await _walletRepository.UpdateAsync(wallet);

                    // Commit transaction
                    await dbTransaction.CommitAsync();

                    _logger.LogInformation("Transação {TransactionId} criada: Tipo: {Type} - Valor: {Amount} - Carteira: {WalletType}",
                        result.Id, type, amount, wallet.WalletType);

                    return MapToDto(result);
                }
                catch (Exception)
                {
                    await dbTransaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar transação para carteira {WalletId}", walletId);
                throw;
            }
        }

        public async Task<WalletTransactionDto> ProcessTransactionAsync(Guid transactionId)
        {
            var transaction = await _transactionRepository.GetByIdAsync(transactionId);
            if (transaction == null)
            {
                throw new NotFoundException($"Transação com ID {transactionId} não encontrada");
            }

            if (transaction.Status != TransactionStatus.Pending)
            {
                throw new ValidationException("Apenas transações pendentes podem ser processadas");
            }

            var wallet = await _walletRepository.GetByIdAsync(transaction.WalletId);
            if (wallet == null)
            {
                throw new NotFoundException($"Carteira com ID {transaction.WalletId} não encontrada");
            }

            // Verificar se o tipo de transação é compatível com o tipo da carteira
            switch (transaction.Type)
            {
                case TransactionType.Credit:
                case TransactionType.Deposit:
                case TransactionType.Refund when wallet.WalletType == WalletType.Withdrawal:
                    // Transações de entrada - apenas permitidas para carteiras Deposit ou General
                    if (wallet.WalletType == WalletType.Withdrawal)
                    {
                        throw new ValidationException($"Transações do tipo {transaction.Type} não são permitidas em carteiras de Saque (Withdrawal)");
                    }
                    break;

                case TransactionType.Debit:
                case TransactionType.Withdraw:
                case TransactionType.Refund when wallet.WalletType == WalletType.Deposit:
                    // Transações de saída - apenas permitidas para carteiras Withdrawal ou General
                    if (wallet.WalletType == WalletType.Deposit)
                    {
                        throw new ValidationException($"Transações do tipo {transaction.Type} não são permitidas em carteiras de Depósito (Deposit)");
                    }
                    break;
            }

            // Verifica novamente o saldo para débitos no momento do processamento
            if ((transaction.Type == TransactionType.Debit || transaction.Type == TransactionType.Withdraw)
                && wallet.AvailableBalance < transaction.Amount)
            {
                transaction.Status = TransactionStatus.Failed;
                await _transactionRepository.UpdateAsync(transaction);
                throw new InsufficientFundsException("Saldo insuficiente para processar a transação");
            }

            try
            {
                await using var dbTransaction = await _walletRepository.BeginTransactionAsync();

                try
                {
                    // Atualiza a transação
                    transaction.Status = TransactionStatus.Completed;
                    transaction.ProcessedAt = DateTime.UtcNow;
                    var result = await _transactionRepository.UpdateAsync(transaction);

                    // Atualiza o saldo da carteira
                    if (transaction.Type == TransactionType.Credit || transaction.Type == TransactionType.Deposit)
                    {
                        wallet.AvailableBalance += transaction.Amount;
                    }
                    else if (transaction.Type == TransactionType.Debit || transaction.Type == TransactionType.Withdraw)
                    {
                        wallet.AvailableBalance -= transaction.Amount;
                    }

                    wallet.TotalBalance = wallet.AvailableBalance + wallet.PendingBalance;
                    wallet.LastUpdateAt = DateTime.UtcNow;
                    await _walletRepository.UpdateAsync(wallet);

                    // Commit da transação
                    await dbTransaction.CommitAsync();

                    _logger.LogInformation("Transação {TransactionId} processada na carteira {WalletType}",
                        transactionId, wallet.WalletType);
                    return MapToDto(result);
                }
                catch (Exception)
                {
                    await dbTransaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar transação {TransactionId}", transactionId);
                throw;
            }
        }

        public async Task<WalletTransactionDto> CancelTransactionAsync(Guid transactionId, string reason = null)
        {
            var transaction = await _transactionRepository.GetByIdAsync(transactionId);
            if (transaction == null)
            {
                throw new NotFoundException($"Transação com ID {transactionId} não encontrada");
            }

            if (transaction.Status != TransactionStatus.Pending)
            {
                throw new ValidationException("Apenas transações pendentes podem ser canceladas");
            }

            var wallet = await _walletRepository.GetByIdAsync(transaction.WalletId);
            if (wallet == null)
            {
                throw new NotFoundException($"Carteira com ID {transaction.WalletId} não encontrada");
            }

            try
            {
                // Para transações de débito pendentes, pode ser necessário restaurar fundos reservados
                if ((transaction.Type == TransactionType.Debit || transaction.Type == TransactionType.Withdraw) &&
                    wallet.PendingBalance >= transaction.Amount)
                {
                    await using var dbTransaction = await _walletRepository.BeginTransactionAsync();

                    try
                    {
                        // Restaura o saldo pendente para disponível se aplicável
                        wallet.PendingBalance -= transaction.Amount;
                        wallet.AvailableBalance += transaction.Amount;
                        wallet.LastUpdateAt = DateTime.UtcNow;
                        await _walletRepository.UpdateAsync(wallet);

                        await dbTransaction.CommitAsync();

                        _logger.LogInformation("Fundos reservados restaurados para a carteira {WalletId} após cancelamento de transação",
                            wallet.Id);
                    }
                    catch (Exception)
                    {
                        await dbTransaction.RollbackAsync();
                        throw;
                    }
                }

                transaction.Status = TransactionStatus.Cancelled;
                if (!string.IsNullOrEmpty(reason))
                {
                    transaction.Description += $" | Cancelada: {reason}";
                }

                var result = await _transactionRepository.UpdateAsync(transaction);
                _logger.LogInformation("Transação {TransactionId} cancelada na carteira {WalletType}. Motivo: {Reason}",
                    transactionId, wallet.WalletType, reason ?? "Não informado");

                return MapToDto(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao cancelar transação {TransactionId}", transactionId);
                throw;
            }
        }

        public async Task<WalletBalanceDto> GetWalletBalanceAsync(Guid walletId)
        {
            try
            {
                var wallet = await _walletRepository.GetByIdAsync(walletId);
                if (wallet == null)
                {
                    throw new NotFoundException($"Carteira com ID {walletId} não encontrada");
                }

                return new WalletBalanceDto
                {
                    WalletId = wallet.Id,
                    AvailableBalance = wallet.AvailableBalance,
                    PendingBalance = wallet.PendingBalance,
                    TotalBalance = wallet.TotalBalance,
                    LastUpdated = wallet.LastUpdateAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter saldo da carteira {WalletId}", walletId);
                throw;
            }
        }

        public async Task<List<WalletTransactionDto>> GetTransactionHistoryAsync(
            Guid walletId,
            DateTime? startDate = null,
            DateTime? endDate = null,
            TransactionType? type = null,
            TransactionStatus? status = null,
            int page = 1,
            int pageSize = 20)
        {
            try
            {
                // Verificar se a carteira existe
                var wallet = await _walletRepository.GetByIdAsync(walletId);
                if (wallet == null)
                {
                    throw new NotFoundException($"Carteira com ID {walletId} não encontrada");
                }

                var transactions = await _transactionRepository.GetByWalletIdAsync(
                    walletId,
                    startDate,
                    endDate,
                    page,
                    pageSize);

                // Aplicar filtros adicionais, se necessário
                var filteredTransactions = transactions;

                if (type.HasValue)
                {
                    filteredTransactions = filteredTransactions
                        .Where(t => t.Type == type.Value)
                        .ToList();
                }

                if (status.HasValue)
                {
                    filteredTransactions = filteredTransactions
                        .Where(t => t.Status == status.Value)
                        .ToList();
                }

                return filteredTransactions.Select(MapToDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter histórico de transações para carteira {WalletId}", walletId);
                throw;
            }
        }

        public async Task<WalletTransactionDto> GetTransactionByIdAsync(Guid transactionId)
        {
            try
            {
                var transaction = await _transactionRepository.GetByIdAsync(transactionId);
                if (transaction == null)
                {
                    throw new NotFoundException($"Transação com ID {transactionId} não encontrada");
                }

                return MapToDto(transaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter transação {TransactionId}", transactionId);
                throw;
            }
        }

        public async Task<IEnumerable<WalletTransactionDto>> GetAllPendingTransactionsAsync(int page = 1, int pageSize = 20)
        {
            var transactions = await _transactionRepository.GetAllPendingTransactionsAsync(page, pageSize);
            return transactions.Select(MapToDto);
        }

        public async Task<int> GetTotalPendingTransactionsAsync()
        {
            return await _transactionRepository.GetPendingTransactionsCountAsync();
        }

        private static WalletTransactionDto MapToDto(WalletTransaction transaction)
        {
            return new WalletTransactionDto
            {
                Id = transaction.Id,
                WalletId = transaction.WalletId,
                Amount = transaction.Amount,
                Type = transaction.Type.ToString(),
                Status = transaction.Status.ToString(),
                Description = transaction.Description,
                Reference = transaction.Reference,
                CreatedAt = transaction.CreatedAt,
                ProcessedAt = transaction.ProcessedAt
            };
        }
    }
}