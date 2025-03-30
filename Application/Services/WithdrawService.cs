using Application.DTOs;
using Application.Interfaces;
using Domain.Interfaces;
using Domain.Models;
using Microsoft.Extensions.Logging;
using Shared.Enums;
using Shared.Exceptions;

namespace Application.Services
{
    public class WithdrawService : IWithdrawService
    {
        private readonly IWithdrawRepository _withdrawRepository;
        private readonly IWalletService _walletService;
        private readonly IWalletRepository _walletRepository;
        private readonly IBankAccountService _bankAccountService;
        private readonly ILogger<WithdrawService> _logger;
        private readonly IListenerService _listenerService;

        public WithdrawService(
            IWithdrawRepository withdrawRepository,
            IWalletService walletService,
            IWalletRepository walletRepository,
            IBankAccountService bankAccountService,
            IListenerService listenerService,
            ILogger<WithdrawService> logger)
        {
            _withdrawRepository = withdrawRepository;
            _walletService = walletService;
            _walletRepository = walletRepository;
            _bankAccountService = bankAccountService;
            _listenerService = listenerService;
            _logger = logger;
        }

        public async Task<WithdrawDto> RequestWithdrawAsync(WithdrawRequestDto request)
        {
            var bankAccount = await _bankAccountService.GetBankAccountAsync(request.BankAccountId);
            if (!bankAccount.IsVerified)
                throw new ValidationException("A conta bancária selecionada não está verificada");

            var wallets = await _walletRepository.GetAllBySellerIdAsync(request.SellerId);

            var withdrawalWallet = wallets.FirstOrDefault(w => w.WalletType == WalletType.Withdrawal);

            var generalWallet = wallets.FirstOrDefault(w => w.WalletType == WalletType.General);

            var requestedWallet = await _walletRepository.GetByIdAsync(request.WalletId);
            if (requestedWallet != null &&
                (requestedWallet.WalletType == WalletType.Withdrawal || requestedWallet.WalletType == WalletType.General))
            {
                withdrawalWallet = requestedWallet;
            }
            else if (requestedWallet != null && requestedWallet.WalletType == WalletType.Deposit)
            {
                throw new ValidationException("Não é possível realizar saques a partir de uma carteira do tipo Depósito (Deposit). Use uma carteira de Saque (Withdrawal) ou Geral (General).");
            }

            var sourceWallet = withdrawalWallet ?? generalWallet;

            if (sourceWallet == null)
            {
                throw new ValidationException("Não foi encontrada uma carteira de Saque (Withdrawal) ou Geral (General) para processar o saque");
            }

            var hasBalance = await _walletService.HasSufficientFundsAsync(sourceWallet.Id, request.Amount);
            if (!hasBalance)
                throw new InsufficientFundsException("Saldo insuficiente para realizar este saque");

            await _walletService.DeductFundsAsync(sourceWallet.Id, new WalletOperationDto
            {
                Amount = request.Amount,
                Description = "Solicitação de saque - Aguardando aprovação",
                Reference = $"WITHDRAW_REQUEST_{Guid.NewGuid()}"
            });

            var withdraw = new Withdraw
            {
                Id = Guid.NewGuid(),
                SellerId = request.SellerId,
                Amount = request.Amount,
                Status = WithdrawStatus.Pending,
                WithdrawMethod = request.Method,
                RequestedAt = DateTime.UtcNow,
                BankAccountId = request.BankAccountId,
                WalletId = sourceWallet.Id
            };

            var result = await _withdrawRepository.CreateAsync(withdraw);
            _logger.LogInformation("Solicitação de saque criada: {WithdrawId} para vendedor {SellerId} - Valor: {Amount} - Carteira: {WalletType}",
                result.Id, result.SellerId, result.Amount, sourceWallet.WalletType);

            return MapToDto(result);
        }

        public async Task<WithdrawDto> GetWithdrawAsync(Guid id)
        {
            var withdraw = await _withdrawRepository.GetByIdAsync(id);
            if (withdraw == null)
                throw new NotFoundException($"Saque com ID {id} não encontrado");

            return MapToDto(withdraw);
        }

        public async Task<IEnumerable<WithdrawDto>> GetWithdrawsBySellerIdAsync(Guid sellerId, int page = 1, int pageSize = 20)
        {
            var withdraws = await _withdrawRepository.GetBySellerIdAsync(sellerId, page, pageSize);
            return withdraws.Select(MapToDto);
        }

        public async Task<IEnumerable<WithdrawDto>> GetPendingWithdrawsAsync(int page = 1, int pageSize = 20)
        {
            var withdraws = await _withdrawRepository.GetByStatusAsync(WithdrawStatus.Pending, page, pageSize);
            return withdraws.Select(MapToDto);
        }

        public async Task<WithdrawDto> ApproveWithdrawAsync(Guid id, string adminId)
        {
            var withdraw = await _withdrawRepository.GetByIdAsync(id);
            if (withdraw == null)
                throw new NotFoundException($"Saque com ID {id} não encontrado");

            if (withdraw.Status != WithdrawStatus.Pending)
                throw new ValidationException($"Não é possível aprovar saque que não está pendente. Status atual: {withdraw.Status}");

            withdraw.Status = WithdrawStatus.Approved;
            withdraw.ApprovedBy = adminId;
            withdraw.ApprovedAt = DateTime.UtcNow;

            var result = await _withdrawRepository.UpdateAsync(withdraw);
            _logger.LogInformation("Saque {WithdrawId} aprovado pelo admin {AdminId}", id, adminId);

            await SendWithdrawNotification(result);

            return MapToDto(result);
        }

        public async Task<WithdrawDto> RejectWithdrawAsync(Guid id, string reason, string adminId)
        {
            var withdraw = await _withdrawRepository.GetByIdAsync(id);
            if (withdraw == null)
                throw new NotFoundException($"Saque com ID {id} não encontrado");

            if (withdraw.Status != WithdrawStatus.Pending)
                throw new ValidationException($"Não é possível rejeitar saque que não está pendente. Status atual: {withdraw.Status}");

            if (withdraw.WalletId != null)
                throw new ValidationException("Não é possível rejeitar saque sem carteira associada");

            await _walletService.AddFundsAsync(withdraw.WalletId, new WalletOperationDto
            {
                Amount = withdraw.Amount,
                Description = $"Estorno de solicitação de saque - Rejeitado: {reason}",
                Reference = $"WITHDRAW_REJECT_{withdraw.Id}"
            });

            withdraw.Status = WithdrawStatus.Rejected;
            withdraw.RejectionReason = reason;
            withdraw.ProcessedAt = DateTime.UtcNow;

            var result = await _withdrawRepository.UpdateAsync(withdraw);
            _logger.LogInformation("Saque {WithdrawId} rejeitado pelo admin {AdminId}. Motivo: {Reason}",
                id, adminId, reason);

            await SendWithdrawNotification(result);

            return MapToDto(result);
        }

        public async Task<WithdrawDto> ProcessWithdrawAsync(Guid id, string transactionReceipt)
        {
            var withdraw = await _withdrawRepository.GetByIdAsync(id);
            if (withdraw == null)
                throw new NotFoundException($"Saque com ID {id} não encontrado");

            if (withdraw.Status != WithdrawStatus.Approved)
                throw new ValidationException($"Não é possível processar saque que não está aprovado. Status atual: {withdraw.Status}");

            withdraw.Status = WithdrawStatus.Completed;
            withdraw.ProcessedAt = DateTime.UtcNow;
            withdraw.TransactionReceipt = transactionReceipt;

            var result = await _withdrawRepository.UpdateAsync(withdraw);
            _logger.LogInformation("Saque {WithdrawId} processado com sucesso. Comprovante: {Receipt}",
                id, transactionReceipt);

            await SendWithdrawNotification(result);

            return MapToDto(result);
        }

        public async Task<int> GetPendingWithdrawsCountAsync()
        {
            return await _withdrawRepository.GetCountByStatusAsync(WithdrawStatus.Pending);
        }

        public async Task<decimal> GetTotalPendingWithdrawAmountAsync()
        {
            return await _withdrawRepository.GetTotalAmountByStatusAsync(WithdrawStatus.Pending);
        }

        private async Task SendWithdrawNotification(Withdraw withdraw)
        {
            try
            {
                var notification = NotificationHelpers.CreateWithdrawNotification(withdraw);
                await _listenerService.GenerateNotification(notification);
                _logger.LogInformation("Withdraw notification sent: {WithdrawId}, Status: {Status}",
                    withdraw.Id, withdraw.Status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending withdraw notification {WithdrawId}", withdraw.Id);
            }
        }

        private WithdrawDto MapToDto(Withdraw withdraw)
        {
            return new WithdrawDto
            {
                Id = withdraw.Id,
                SellerId = withdraw.SellerId,
                Amount = withdraw.Amount,
                Status = withdraw.Status.ToString(),
                WithdrawMethod = withdraw.WithdrawMethod,
                RequestedAt = withdraw.RequestedAt,
                ProcessedAt = withdraw.ProcessedAt,
                BankAccountId = withdraw.BankAccountId,
                BankAccount = withdraw.BankAccount != null ? MapBankAccountToDto(withdraw.BankAccount) : null,
                RejectionReason = withdraw.RejectionReason,
                TransactionReceipt = withdraw.TransactionReceipt,
                ApprovedAt = withdraw.ApprovedAt
            };
        }

        private BankAccountDto MapBankAccountToDto(BankAccount bankAccount)
        {
            return new BankAccountDto
            {
                Id = bankAccount.Id,
                BankName = bankAccount.BankName,
                AccountType = bankAccount.AccountType.ToString(),
                AccountNumber = bankAccount.AccountNumber,
                BranchNumber = bankAccount.BranchNumber,
                PixKey = bankAccount.PixKey,
                PixKeyType = bankAccount.PixKeyType?.ToString(),
                AccountHolderName = bankAccount.AccountHolderName
            };
        }
    }
}
