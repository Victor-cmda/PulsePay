using Application.DTOs;
using Application.Interfaces;
using Domain.Interfaces;
using Domain.Models;
using Microsoft.Extensions.Logging;
using Shared.Exceptions;

namespace Application.Services
{
    public class WithdrawService : IWithdrawService
    {
        private readonly IWithdrawRepository _withdrawRepository;
        private readonly IWalletService _walletService;
        private readonly ILogger<WithdrawService> _logger;

        public WithdrawService(
            IWithdrawRepository withdrawRepository,
            IWalletService walletService,
            ILogger<WithdrawService> logger)
        {
            _withdrawRepository = withdrawRepository;
            _walletService = walletService;
            _logger = logger;
        }

        public async Task<WithdrawResponseDto> RequestWithdrawAsync(WithdrawCreateDto createDto)
        {
            // Verificar se há saldo disponível
            var hasSufficientFunds = await _walletService.HasSufficientFundsAsync(createDto.SellerId, createDto.Amount);
            if (!hasSufficientFunds)
                throw new InsufficientFundsException("Insufficient funds for withdrawal");

            // Criar o saque
            var withdraw = new Withdraw
            {
                SellerId = createDto.SellerId,
                Amount = createDto.Amount,
                Status = "Pending",
                WithdrawMethod = createDto.WithdrawMethod,
                BankAccountId = createDto.BankAccountId
            };

            // Deduzir o valor da carteira
            await _walletService.DeductFundsAsync(createDto.SellerId, createDto.Amount);

            // Salvar o saque
            var created = await _withdrawRepository.CreateAsync(withdraw);
            _logger.LogInformation($"Withdraw requested: {created.Id} for seller {created.SellerId}");

            return MapToResponseDto(created);
        }

        public async Task<WithdrawResponseDto> GetWithdrawAsync(Guid id)
        {
            var withdraw = await _withdrawRepository.GetByIdAsync(id);
            if (withdraw == null)
                throw new NotFoundException($"Withdraw {id} not found");

            return MapToResponseDto(withdraw);
        }

        public async Task<IEnumerable<WithdrawResponseDto>> GetWithdrawsBySellerAsync(Guid sellerId, int page = 1, int pageSize = 10)
        {
            var withdraws = await _withdrawRepository.GetBySellerIdAsync(sellerId, page, pageSize);
            return withdraws.Select(MapToResponseDto);
        }

        public async Task<WithdrawResponseDto> ProcessWithdrawAsync(Guid id, WithdrawUpdateDto updateDto)
        {
            var withdraw = await _withdrawRepository.GetByIdAsync(id);
            if (withdraw == null)
                throw new NotFoundException($"Withdraw {id} not found");

            if (withdraw.Status != "Pending")
                throw new InvalidOperationException("Only pending withdraws can be processed");

            withdraw.Status = updateDto.Status;
            withdraw.FailureReason = updateDto.FailureReason;
            withdraw.TransactionReceipt = updateDto.TransactionReceipt;

            // Se o saque falhou, devolver o dinheiro para a carteira
            if (updateDto.Status == "Failed")
            {
                await _walletService.AddFundsAsync(withdraw.SellerId, withdraw.Amount);
                _logger.LogInformation($"Funds returned to wallet for failed withdraw {withdraw.Id}");
            }

            var updated = await _withdrawRepository.UpdateAsync(withdraw);
            _logger.LogInformation($"Withdraw {updated.Id} processed with status: {updated.Status}");

            return MapToResponseDto(updated);
        }

        public async Task<WithdrawSummaryDto> GetWithdrawSummaryAsync(Guid sellerId, DateTime startDate, DateTime endDate)
        {
            var totalWithdrawn = await _withdrawRepository.GetTotalWithdrawnAmountAsync(sellerId, startDate, endDate);

            var withdraws = await _withdrawRepository.GetBySellerIdAsync(sellerId);
            var pendingAmount = withdraws
                .Where(w => w.Status == "Pending")
                .Sum(w => w.Amount);

            return new WithdrawSummaryDto
            {
                TotalWithdrawn = totalWithdrawn,
                TotalRequests = withdraws.Count(),
                PendingAmount = pendingAmount,
                Period = startDate
            };
        }

        public async Task<IEnumerable<WithdrawResponseDto>> GetPendingWithdrawsAsync()
        {
            var pendingWithdraws = await _withdrawRepository.GetPendingWithdrawsAsync();
            return pendingWithdraws.Select(MapToResponseDto);
        }

        private static WithdrawResponseDto MapToResponseDto(Withdraw withdraw)
        {
            return new WithdrawResponseDto
            {
                Id = withdraw.Id,
                SellerId = withdraw.SellerId,
                Amount = withdraw.Amount,
                Status = withdraw.Status,
                WithdrawMethod = withdraw.WithdrawMethod,
                RequestedAt = withdraw.RequestedAt,
                ProcessedAt = withdraw.ProcessedAt,
                BankAccount = new BankAccount
                {
                    Id = withdraw.BankAccount.Id,
                    BankName = withdraw.BankAccount.BankName,
                    AccountType = withdraw.BankAccount.AccountType,
                    AccountNumber = withdraw.BankAccount.AccountNumber,
                    BranchNumber = withdraw.BankAccount.BranchNumber,
                    PIXKeyType = withdraw.BankAccount.PIXKeyType,
                    PIXKey = withdraw.BankAccount.PIXKey,
                },
                FailureReason = withdraw.FailureReason,
                TransactionReceipt = withdraw.TransactionReceipt
            };
        }
    }

}
