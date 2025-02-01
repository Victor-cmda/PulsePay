using Application.DTOs;
using Application.Interfaces;
using Domain.Interfaces;
using Domain.Models;
using Microsoft.Extensions.Logging;
using Shared.Exceptions;
using System.ComponentModel.DataAnnotations;
using ValidationException = Shared.Exceptions.ValidationException;

namespace Application.Services
{
    public class WalletService : IWalletService
    {
        private readonly IWalletRepository _walletRepository;
        private readonly ILogger<WalletService> _logger;

        public WalletService(
            IWalletRepository walletRepository,
            ILogger<WalletService> logger)
        {
            _walletRepository = walletRepository;
            _logger = logger;
        }

        public async Task<WalletResponseDto> GetWalletAsync(Guid sellerId)
        {
            var wallet = await _walletRepository.GetBySellerIdAsync(sellerId);
            if (wallet == null)
                throw new NotFoundException($"Wallet not found for seller {sellerId}");

            return MapToResponseDto(wallet);
        }

        public async Task<WalletResponseDto> CreateWalletAsync(WalletCreateDto createDto)
        {
            var exists = await _walletRepository.ExistsAsync(createDto.SellerId);
            if (exists)
                throw new ConflictException($"Wallet already exists for seller {createDto.SellerId}");

            var wallet = new Wallet
            {
                SellerId = createDto.SellerId,
                AvailableBalance = 0,
                PendingBalance = 0,
                TotalBalance = 0
            };

            var created = await _walletRepository.CreateAsync(wallet);
            return MapToResponseDto(created);
        }

        public async Task<WalletResponseDto> UpdateBalanceAsync(Guid sellerId, WalletUpdateDto updateDto)
        {
            var wallet = await _walletRepository.GetBySellerIdAsync(sellerId);
            if (wallet == null)
                throw new NotFoundException($"Wallet not found for seller {sellerId}");

            wallet.AvailableBalance = updateDto.AvailableBalance;
            wallet.PendingBalance = updateDto.PendingBalance;
            wallet.TotalBalance = wallet.AvailableBalance + wallet.PendingBalance;

            var updated = await _walletRepository.UpdateAsync(wallet);
            return MapToResponseDto(updated);
        }

        public async Task<WalletResponseDto> AddFundsAsync(Guid sellerId, decimal amount)
        {
            if (amount <= 0)
                throw new ValidationException("Amount must be greater than zero");

            var wallet = await _walletRepository.GetBySellerIdAsync(sellerId);
            if (wallet == null)
                throw new NotFoundException($"Wallet not found for seller {sellerId}");

            wallet.AvailableBalance += amount;
            wallet.TotalBalance = wallet.AvailableBalance + wallet.PendingBalance;

            var updated = await _walletRepository.UpdateAsync(wallet);
            _logger.LogInformation($"Added {amount} to wallet {wallet.Id}");

            return MapToResponseDto(updated);
        }

        public async Task<WalletResponseDto> DeductFundsAsync(Guid sellerId, decimal amount)
        {
            if (amount <= 0)
                throw new ValidationException("Amount must be greater than zero");

            var wallet = await _walletRepository.GetBySellerIdAsync(sellerId);
            if (wallet == null)
                throw new NotFoundException($"Wallet not found for seller {sellerId}");

            if (wallet.AvailableBalance < amount)
                throw new InsufficientFundsException("Insufficient funds for this operation");

            wallet.AvailableBalance -= amount;
            wallet.TotalBalance = wallet.AvailableBalance + wallet.PendingBalance;

            var updated = await _walletRepository.UpdateAsync(wallet);
            _logger.LogInformation($"Deducted {amount} from wallet {wallet.Id}");

            return MapToResponseDto(updated);
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

        private static WalletResponseDto MapToResponseDto(Wallet wallet)
        {
            return new WalletResponseDto
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
    }
}
