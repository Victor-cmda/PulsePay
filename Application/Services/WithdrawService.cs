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
        private readonly IBankAccountService _bankAccountService;
        private readonly ILogger<WithdrawService> _logger;

        public WithdrawService(
            IWithdrawRepository withdrawRepository,
            IWalletService walletService,
            IBankAccountService bankAccountService,
            ILogger<WithdrawService> logger)
        {
            _withdrawRepository = withdrawRepository;
            _walletService = walletService;
            _bankAccountService = bankAccountService;
            _logger = logger;
        }

        public async Task<WithdrawDto> RequestWithdrawAsync(WithdrawRequestDto request)
        {
            // Verificar se a conta bancária existe e está verificada
            var bankAccount = await _bankAccountService.GetBankAccountAsync(request.BankAccountId);
            if (!bankAccount.IsVerified)
                throw new ValidationException("A conta bancária selecionada não está verificada");

            // Verificar se o saldo é suficiente
            var hasBalance = await _walletService.HasSufficientFundsAsync(request.WalletId, request.Amount);
            if (!hasBalance)
                throw new InsufficientFundsException("Saldo insuficiente para realizar este saque");

            // Deduzir o valor da carteira (ou colocar em pending, dependendo da sua lógica)
            await _walletService.DeductFundsAsync(request.WalletId, new WalletOperationDto
            {
                Amount = request.Amount,
                Description = "Solicitação de saque - Aguardando aprovação",
                Reference = $"WITHDRAW_REQUEST_{Guid.NewGuid()}"
            });

            // Criar a solicitação de saque
            var withdraw = new Withdraw
            {
                Id = Guid.NewGuid(),
                SellerId = request.SellerId,
                Amount = request.Amount,
                Status = WithdrawStatus.Pending,
                WithdrawMethod = request.Method, // PIX, TED, etc.
                RequestedAt = DateTime.UtcNow,
                BankAccountId = request.BankAccountId,
            };

            var result = await _withdrawRepository.CreateAsync(withdraw);
            _logger.LogInformation("Solicitação de saque criada: {WithdrawId} para vendedor {SellerId} - Valor: {Amount}",
                result.Id, result.SellerId, result.Amount);

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
