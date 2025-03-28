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

        public WithdrawService(
            IWithdrawRepository withdrawRepository,
            IWalletService walletService,
            IWalletRepository walletRepository,
            IBankAccountService bankAccountService,
            ILogger<WithdrawService> logger)
        {
            _withdrawRepository = withdrawRepository;
            _walletService = walletService;
            _walletRepository = walletRepository;
            _bankAccountService = bankAccountService;
            _logger = logger;
        }

        public async Task<WithdrawDto> RequestWithdrawAsync(WithdrawRequestDto request)
        {
            // Verify if the bank account exists and is verified
            var bankAccount = await _bankAccountService.GetBankAccountAsync(request.BankAccountId);
            if (!bankAccount.IsVerified)
                throw new ValidationException("A conta bancária selecionada não está verificada");

            // Find the appropriate withdrawal wallet
            var wallets = await _walletRepository.GetAllBySellerIdAsync(request.SellerId);

            // First try to find a Withdrawal wallet
            var withdrawalWallet = wallets.FirstOrDefault(w => w.WalletType == WalletType.Withdrawal);

            // If no Withdrawal wallet exists, try to use a General wallet
            var generalWallet = wallets.FirstOrDefault(w => w.WalletType == WalletType.General);

            // Use the specified wallet ID if it's a Withdrawal or General wallet
            var requestedWallet = await _walletRepository.GetByIdAsync(request.WalletId);
            if (requestedWallet != null &&
                (requestedWallet.WalletType == WalletType.Withdrawal || requestedWallet.WalletType == WalletType.General))
            {
                // Use the requested wallet if it's of appropriate type
                withdrawalWallet = requestedWallet;
            }
            else if (requestedWallet != null && requestedWallet.WalletType == WalletType.Deposit)
            {
                // Cannot withdraw from a Deposit wallet
                throw new ValidationException("Não é possível realizar saques a partir de uma carteira do tipo Depósito (Deposit). Use uma carteira de Saque (Withdrawal) ou Geral (General).");
            }

            // Determine which wallet to use for the withdrawal
            var sourceWallet = withdrawalWallet ?? generalWallet;

            if (sourceWallet == null)
            {
                throw new ValidationException("Não foi encontrada uma carteira de Saque (Withdrawal) ou Geral (General) para processar o saque");
            }

            // Check if the balance is sufficient
            var hasBalance = await _walletService.HasSufficientFundsAsync(sourceWallet.Id, request.Amount);
            if (!hasBalance)
                throw new InsufficientFundsException("Saldo insuficiente para realizar este saque");

            // Deduct the amount from the wallet
            await _walletService.DeductFundsAsync(sourceWallet.Id, new WalletOperationDto
            {
                Amount = request.Amount,
                Description = "Solicitação de saque - Aguardando aprovação",
                Reference = $"WITHDRAW_REQUEST_{Guid.NewGuid()}"
            });

            // Create the withdrawal request
            var withdraw = new Withdraw
            {
                Id = Guid.NewGuid(),
                SellerId = request.SellerId,
                Amount = request.Amount,
                Status = WithdrawStatus.Pending,
                WithdrawMethod = request.Method, // PIX, TED, etc.
                RequestedAt = DateTime.UtcNow,
                BankAccountId = request.BankAccountId,
                WalletId = sourceWallet.Id  // Store which wallet was used
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
