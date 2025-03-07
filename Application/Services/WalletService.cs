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
    public class WalletService : IWalletService
    {
        private readonly IWalletRepository _walletRepository;
        private readonly IWalletTransactionRepository _transactionRepository;
        private readonly ILogger<WalletService> _logger;

        public WalletService(
            IWalletRepository walletRepository,
            IWalletTransactionRepository transactionRepository,
            ILogger<WalletService> logger)
        {
            _walletRepository = walletRepository ?? throw new ArgumentNullException(nameof(walletRepository));
            _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<WalletDto> GetWalletAsync(Guid sellerId)
        {
            var wallet = await _walletRepository.GetBySellerIdAsync(sellerId);
            if (wallet == null)
                throw new NotFoundException($"Carteira não encontrada para o vendedor {sellerId}");

            return MapToDto(wallet);
        }

        public async Task<WalletWithTransactionsDto> GetWalletWithRecentTransactionsAsync(Guid sellerId, int count = 10)
        {
            var wallet = await _walletRepository.GetBySellerIdAsync(sellerId);
            if (wallet == null)
                throw new NotFoundException($"Carteira não encontrada para o vendedor {sellerId}");

            var transactions = await _transactionRepository.GetRecentByWalletIdAsync(wallet.Id, count);

            return new WalletWithTransactionsDto
            {
                Wallet = MapToDto(wallet),
                RecentTransactions = transactions.Select(MapTransactionToDto).ToList()
            };
        }

        public async Task<WalletDto> CreateWalletAsync(WalletCreateDto createDto)
        {
            var exists = await _walletRepository.ExistsAsync(createDto.SellerId);
            if (exists)
                throw new ConflictException($"Já existe uma carteira para o vendedor {createDto.SellerId}");

            var wallet = new Wallet
            {
                Id = Guid.NewGuid(),
                SellerId = createDto.SellerId,
                AvailableBalance = 0,
                PendingBalance = 0,
                TotalBalance = 0,
                CreatedAt = DateTime.UtcNow,
                LastUpdateAt = DateTime.UtcNow
            };

            var created = await _walletRepository.CreateAsync(wallet);
            _logger.LogInformation("Carteira criada para o vendedor {SellerId}", createDto.SellerId);

            return MapToDto(created);
        }

        public async Task<WalletDto> UpdateBalanceAsync(Guid sellerId, WalletUpdateDto updateDto)
        {
            var wallet = await _walletRepository.GetBySellerIdAsync(sellerId);
            if (wallet == null)
                throw new NotFoundException($"Carteira não encontrada para o vendedor {sellerId}");

            wallet.AvailableBalance = updateDto.AvailableBalance;
            wallet.PendingBalance = updateDto.PendingBalance;
            wallet.TotalBalance = wallet.AvailableBalance + wallet.PendingBalance;
            wallet.LastUpdateAt = DateTime.UtcNow;

            var updated = await _walletRepository.UpdateAsync(wallet);
            _logger.LogInformation("Saldo atualizado para a carteira do vendedor {SellerId}", sellerId);

            return MapToDto(updated);
        }

        public async Task<WalletDto> AddFundsAsync(Guid sellerId, WalletOperationDto operationDto)
        {
            if (operationDto.Amount <= 0)
                throw new ValidationException("O valor deve ser maior que zero");

            var wallet = await _walletRepository.GetBySellerIdAsync(sellerId);
            if (wallet == null)
                throw new NotFoundException($"Carteira não encontrada para o vendedor {sellerId}");

            // Criar a transação
            var transaction = new WalletTransaction
            {
                Id = Guid.NewGuid(),
                WalletId = wallet.Id,
                Amount = operationDto.Amount,
                Type = TransactionType.Deposit,
                Status = TransactionStatus.Completed,
                Description = operationDto.Description ?? "Depósito de fundos",
                Reference = operationDto.Reference,
                CreatedAt = DateTime.UtcNow,
                ProcessedAt = DateTime.UtcNow
            };

            // Atualizar o saldo da carteira
            wallet.AvailableBalance += operationDto.Amount;
            wallet.TotalBalance = wallet.AvailableBalance + wallet.PendingBalance;
            wallet.LastUpdateAt = DateTime.UtcNow;

            // Salvar as alterações em transação
            await using var transaction1 = await _walletRepository.BeginTransactionAsync();
            try
            {
                await _transactionRepository.CreateAsync(transaction);
                var updated = await _walletRepository.UpdateAsync(wallet);

                await transaction1.CommitAsync();

                _logger.LogInformation("Adicionado {Amount} à carteira {WalletId}", operationDto.Amount, wallet.Id);
                return MapToDto(updated);
            }
            catch (Exception ex)
            {
                await transaction1.RollbackAsync();
                _logger.LogError(ex, "Erro ao adicionar fundos à carteira {WalletId}", wallet.Id);
                throw;
            }
        }

        public async Task<WalletDto> DeductFundsAsync(Guid sellerId, WalletOperationDto operationDto)
        {
            if (operationDto.Amount <= 0)
                throw new ValidationException("O valor deve ser maior que zero");

            var wallet = await _walletRepository.GetBySellerIdAsync(sellerId);
            if (wallet == null)
                throw new NotFoundException($"Carteira não encontrada para o vendedor {sellerId}");

            if (wallet.AvailableBalance < operationDto.Amount)
                throw new InsufficientFundsException("Saldo insuficiente para esta operação");

            // Criar a transação
            var transaction = new WalletTransaction
            {
                Id = Guid.NewGuid(),
                WalletId = wallet.Id,
                Amount = operationDto.Amount,
                Type = TransactionType.Withdraw,
                Status = TransactionStatus.Completed,
                Description = operationDto.Description ?? "Retirada de fundos",
                Reference = operationDto.Reference,
                CreatedAt = DateTime.UtcNow,
                ProcessedAt = DateTime.UtcNow
            };

            // Atualizar o saldo da carteira
            wallet.AvailableBalance -= operationDto.Amount;
            wallet.TotalBalance = wallet.AvailableBalance + wallet.PendingBalance;
            wallet.LastUpdateAt = DateTime.UtcNow;

            // Salvar as alterações em transação
            await using var transaction1 = await _walletRepository.BeginTransactionAsync();
            try
            {
                await _transactionRepository.CreateAsync(transaction);
                var updated = await _walletRepository.UpdateAsync(wallet);

                await transaction1.CommitAsync();

                _logger.LogInformation("Deduzido {Amount} da carteira {WalletId}", operationDto.Amount, wallet.Id);
                return MapToDto(updated);
            }
            catch (Exception ex)
            {
                await transaction1.RollbackAsync();
                _logger.LogError(ex, "Erro ao deduzir fundos da carteira {WalletId}", wallet.Id);
                throw;
            }
        }

        public async Task<List<WalletTransactionDto>> GetTransactionsAsync(Guid sellerId, DateTime? startDate = null, DateTime? endDate = null, int page = 1, int pageSize = 20)
        {
            var wallet = await _walletRepository.GetBySellerIdAsync(sellerId);
            if (wallet == null)
                throw new NotFoundException($"Carteira não encontrada para o vendedor {sellerId}");

            var transactions = await _transactionRepository.GetByWalletIdAsync(
                wallet.Id,
                startDate,
                endDate,
                page,
                pageSize);

            return transactions.Select(MapTransactionToDto).ToList();
        }

        public async Task<bool> HasSufficientFundsAsync(Guid sellerId, decimal amount)
        {
            var wallet = await _walletRepository.GetBySellerIdAsync(sellerId);
            return wallet?.AvailableBalance >= amount;
        }

        public async Task<decimal> GetAvailableBalanceAsync(Guid sellerId)
        {
            var wallet = await _walletRepository.GetBySellerIdAsync(sellerId);
            return wallet?.AvailableBalance ?? 0;
        }

        private static WalletDto MapToDto(Wallet wallet)
        {
            return new WalletDto
            {
                Id = wallet.Id,
                SellerId = wallet.SellerId,
                AvailableBalance = wallet.AvailableBalance,
                PendingBalance = wallet.PendingBalance,
                TotalBalance = wallet.TotalBalance,
                LastUpdateAt = wallet.LastUpdateAt,
                CreatedAt = wallet.CreatedAt
            };
        }

        private static WalletTransactionDto MapTransactionToDto(WalletTransaction transaction)
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