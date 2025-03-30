using Application.DTOs;
using Application.Interfaces;
using Domain.Interfaces;
using Domain.Models;
using Microsoft.Extensions.Logging;
using Shared.Enums;
using Shared.Exceptions;

namespace Application.Services
{
    public class RefundService : IRefundService
    {
        private readonly IRefundRepository _refundRepository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly IWalletRepository _walletRepository;
        private readonly IListenerService _listenerService;
        private readonly IWalletService _walletService;
        private readonly ILogger<RefundService> _logger;

        public RefundService(
            IRefundRepository refundRepository,
            ITransactionRepository transactionRepository,
            IListenerService listenerService,
            IWalletRepository walletRepository,
            IWalletService walletService,
            ILogger<RefundService> logger)
        {
            _refundRepository = refundRepository;
            _transactionRepository = transactionRepository;
            _listenerService = listenerService;
            _walletRepository = walletRepository;
            _walletService = walletService;
            _logger = logger;
        }

        public async Task<RefundResponseDto> RequestRefundAsync(RefundRequestDto request, Guid sellerId)
        {

            var transaction = await _transactionRepository.GetByIdAsync(Guid.Parse(request.Transaction_id));
            if (transaction == null)
                throw new NotFoundException($"Transação com ID {request.Transaction_id} não encontrada");

            if (transaction.SellerId != sellerId)
                throw new NotFoundException($"Transação com ID {request.Transaction_id} não encontrada");

            if (transaction.Status != "COMPLETED" && transaction.Status != "APPROVED")
                throw new ValidationException("Apenas transações completadas podem ser estornadas");

            if (request.Amount > transaction.Amount)
                throw new ValidationException("O valor do estorno não pode ser maior que o valor da transação");

            var wallets = await _walletRepository.GetAllBySellerIdAsync(sellerId);

            var withdrawalWallet = wallets.FirstOrDefault(w => w.WalletType == WalletType.Withdrawal);

            var generalWallet = wallets.FirstOrDefault(w => w.WalletType == WalletType.General);

            var refundWallet = withdrawalWallet ?? generalWallet;

            if (refundWallet == null)
            {
                throw new ValidationException("Não foi encontrada uma carteira de Saque (Withdrawal) ou Geral (General) para processar o estorno");
            }

            if (refundWallet.AvailableBalance < request.Amount)
            {
                throw new InsufficientFundsException($"Saldo insuficiente na carteira de {refundWallet.WalletType} para processar o estorno");
            }

            var refund = new Refund
            {
                Id = Guid.NewGuid(),
                SellerId = sellerId,
                TransactionId = Guid.Parse(request.Transaction_id),
                Amount = request.Amount,
                Status = RefundStatus.Pending,
                Reason = request.Reason,
                ExternalReference = "",
                CreatedAt = DateTime.UtcNow
            };

            var result = await _refundRepository.CreateAsync(refund);
            _logger.LogInformation("Estorno criado: {RefundId} para transação {TransactionId} - Valor: {Amount} - Carteira: {WalletType}",
                result.Id, result.TransactionId, result.Amount, refundWallet.WalletType);

            await SendRefundNotification(result, "refund.created");

            return MapToDto(result);
        }

        public async Task<RefundResponseDto> GetRefundStatusAsync(Guid id)
        {
            var refund = await _refundRepository.GetByIdAsync(id);
            if (refund == null)
                throw new NotFoundException($"Estorno com ID {id} não encontrado");

            return MapToDto(refund);
        }

        public async Task<IEnumerable<RefundResponseDto>> GetRefundsBySellerIdAsync(Guid sellerId, int page = 1, int pageSize = 20)
        {
            var refunds = await _refundRepository.GetBySellerIdAsync(sellerId, page, pageSize);
            return refunds.Select(MapToDto);
        }

        public async Task<RefundResponseDto> ApproveRefundAsync(Guid id, string adminId)
        {
            var refund = await _refundRepository.GetByIdAsync(id);
            if (refund == null)
                throw new NotFoundException($"Estorno com ID {id} não encontrado");

            if (refund.Status != RefundStatus.Pending)
                throw new ValidationException($"Não é possível aprovar estorno que não está pendente. Status atual: {refund.Status}");

            var wallets = await _walletRepository.GetAllBySellerIdAsync(refund.SellerId);

            var withdrawalWallet = wallets.FirstOrDefault(w => w.WalletType == WalletType.Withdrawal);

            var generalWallet = wallets.FirstOrDefault(w => w.WalletType == WalletType.General);

            var refundWallet = withdrawalWallet ?? generalWallet;

            if (refundWallet == null)
            {
                throw new ValidationException("Não foi encontrada uma carteira de Saque (Withdrawal) ou Geral (General) para processar o estorno");
            }

            if (refundWallet.AvailableBalance < refund.Amount)
            {
                throw new InsufficientFundsException($"Saldo insuficiente na carteira de {refundWallet.WalletType} para processar o estorno");
            }

            await _walletService.DeductFundsAsync(refundWallet.Id, new WalletOperationDto
            {
                Amount = refund.Amount,
                Description = $"Estorno aprovado - {refund.Reason}",
                Reference = $"REFUND_APPROVED_{refund.Id}"
            });

            refund.Status = RefundStatus.Processing;
            refund.ProcessedAt = DateTime.UtcNow;
            refund.RefundWalletId = refundWallet.Id;

            var result = await _refundRepository.UpdateAsync(refund);
            _logger.LogInformation("Estorno {RefundId} aprovado pelo admin {AdminId} usando carteira {WalletType}",
                id, adminId, refundWallet.WalletType);

            await SendRefundNotification(result, "refund.processing");

            return MapToDto(result);
        }

        public async Task<RefundResponseDto> RejectRefundAsync(Guid id, string reason, string adminId)
        {
            var refund = await _refundRepository.GetByIdAsync(id);
            if (refund == null)
                throw new NotFoundException($"Estorno com ID {id} não encontrado");

            if (refund.Status != RefundStatus.Pending)
                throw new ValidationException($"Não é possível rejeitar estorno que não está pendente. Status atual: {refund.Status}");

            // Atualizar status para Failed
            refund.Status = RefundStatus.Failed;
            refund.FailReason = reason;
            refund.ProcessedAt = DateTime.UtcNow;

            var result = await _refundRepository.UpdateAsync(refund);
            _logger.LogInformation("Estorno {RefundId} rejeitado pelo admin {AdminId}. Motivo: {Reason}",
                id, adminId, reason);

            // Enviar notificação de estorno rejeitado
            await SendRefundNotification(result, "refund.failed");

            return MapToDto(result);
        }

        public async Task<RefundResponseDto> CompleteRefundAsync(Guid id, string transactionReceipt)
        {
            var refund = await _refundRepository.GetByIdAsync(id);
            if (refund == null)
                throw new NotFoundException($"Estorno com ID {id} não encontrado");

            if (refund.Status != RefundStatus.Processing)
                throw new ValidationException($"Não é possível completar estorno que não está em processamento. Status atual: {refund.Status}");

            // Atualizar status para Completed
            refund.Status = RefundStatus.Completed;
            refund.ProcessedAt = DateTime.UtcNow;
            refund.TransactionReceipt = transactionReceipt;

            var result = await _refundRepository.UpdateAsync(refund);
            _logger.LogInformation("Estorno {RefundId} completado com sucesso.", id);

            // Enviar notificação de estorno completado
            await SendRefundNotification(result, "refund.completed");

            return MapToDto(result);
        }

        private async Task SendRefundNotification(Refund refund, string eventType)
        {
            try
            {
                var notification = RefundNotificationHelper.CreateRefundNotification(refund, eventType);

                await _listenerService.GenerateNotification(notification);
                _logger.LogInformation("Refund notification sent: {RefundId}, Status: {Status}, Event: {EventType}",
                    refund.Id, refund.Status, eventType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending refund notification {RefundId}", refund.Id);
            }
        }

        public async Task<IEnumerable<RefundResponseDto>> GetPendingRefundsAsync(int page = 1, int pageSize = 20)
        {
            var refunds = await _refundRepository.GetByStatusAsync(RefundStatus.Pending, page, pageSize);
            return refunds.Select(MapToDto);
        }

        private RefundResponseDto MapToDto(Refund refund)
        {
            return new RefundResponseDto
            {
                refund_id = refund.Id,
                Transaction_id = refund.TransactionId,
                Amount = refund.Amount,
                Status = refund.Status.ToString(),
                Reason = refund.Reason,
                created_at = refund.CreatedAt,
                ProcessedAt = refund.ProcessedAt,
            };
        }
    }
}