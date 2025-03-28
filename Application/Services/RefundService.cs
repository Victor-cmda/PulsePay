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
            // Verify if the transaction exists
            var transaction = await _transactionRepository.GetByIdAsync(request.TransactionId);
            if (transaction == null)
                throw new NotFoundException($"Transação com ID {request.TransactionId} não encontrada");

            // Verify if the transaction belongs to the seller
            if (transaction.SellerId != sellerId)
                throw new NotFoundException($"Transação com ID {request.TransactionId} não encontrada");

            // Verify if the transaction is completed (can only refund completed transactions)
            if (transaction.Status != "COMPLETED" && transaction.Status != "APPROVED")
                throw new ValidationException("Apenas transações completadas podem ser estornadas");

            // Verify if the refund amount does not exceed the transaction amount
            if (request.Amount > transaction.Amount)
                throw new ValidationException("O valor do estorno não pode ser maior que o valor da transação");

            // Find the appropriate withdrawal wallet for the refund
            var wallets = await _walletRepository.GetAllBySellerIdAsync(sellerId);

            // Try to find a Withdrawal wallet first
            var withdrawalWallet = wallets.FirstOrDefault(w => w.WalletType == WalletType.Withdrawal);

            // If no Withdrawal wallet exists, try to use a General wallet
            var generalWallet = wallets.FirstOrDefault(w => w.WalletType == WalletType.General);

            // Determine which wallet to use for the refund
            var refundWallet = withdrawalWallet ?? generalWallet;

            if (refundWallet == null)
            {
                throw new ValidationException("Não foi encontrada uma carteira de Saque (Withdrawal) ou Geral (General) para processar o estorno");
            }

            // Check if the refund wallet has sufficient balance
            if (refundWallet.AvailableBalance < request.Amount)
            {
                throw new InsufficientFundsException($"Saldo insuficiente na carteira de {refundWallet.WalletType} para processar o estorno");
            }

            // Create the refund
            var refund = new Refund
            {
                Id = Guid.NewGuid(),
                SellerId = sellerId,
                TransactionId = request.TransactionId,
                Amount = request.Amount,
                Status = RefundStatus.Pending,
                Reason = request.Reason,
                ExternalReference = request.ExternalReference,
                CreatedAt = DateTime.UtcNow
            };

            // Save the refund
            var result = await _refundRepository.CreateAsync(refund);
            _logger.LogInformation("Estorno criado: {RefundId} para transação {TransactionId} - Valor: {Amount} - Carteira: {WalletType}",
                result.Id, result.TransactionId, result.Amount, refundWallet.WalletType);

            // Send refund notification
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

            // Verificar se existe uma carteira adequada para processar o estorno
            var wallets = await _walletRepository.GetAllBySellerIdAsync(refund.SellerId);

            // Primeiro tenta encontrar uma carteira Withdrawal
            var withdrawalWallet = wallets.FirstOrDefault(w => w.WalletType == WalletType.Withdrawal);

            // Se não encontrar, tenta usar uma carteira General
            var generalWallet = wallets.FirstOrDefault(w => w.WalletType == WalletType.General);

            // Determina qual carteira usar para o estorno
            var refundWallet = withdrawalWallet ?? generalWallet;

            if (refundWallet == null)
            {
                throw new ValidationException("Não foi encontrada uma carteira de Saque (Withdrawal) ou Geral (General) para processar o estorno");
            }

            // Verificar se a carteira tem saldo suficiente
            if (refundWallet.AvailableBalance < refund.Amount)
            {
                throw new InsufficientFundsException($"Saldo insuficiente na carteira de {refundWallet.WalletType} para processar o estorno");
            }

            // Deduzir o valor da carteira
            await _walletService.DeductFundsAsync(refundWallet.Id, new WalletOperationDto
            {
                Amount = refund.Amount,
                Description = $"Estorno aprovado - {refund.Reason}",
                Reference = $"REFUND_APPROVED_{refund.Id}"
            });

            // Atualizar status para Processing
            refund.Status = RefundStatus.Processing;
            refund.ProcessedAt = DateTime.UtcNow;
            refund.RefundWalletId = refundWallet.Id; // Adicione este campo ao modelo Refund

            var result = await _refundRepository.UpdateAsync(refund);
            _logger.LogInformation("Estorno {RefundId} aprovado pelo admin {AdminId} usando carteira {WalletType}",
                id, adminId, refundWallet.WalletType);

            // Enviar notificação de estorno aprovado
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
                // Criar a notificação
                var notification = new NotificationDto
                {
                    TransactionId = refund.TransactionId.ToString(),
                    PaymentId = refund.Id.ToString(),
                    PaymentType = "REFUND",
                    CustomerId = refund.SellerId.ToString(),
                    Status = refund.Status.ToString(),
                    Description = $"Estorno {refund.Status} - {refund.Reason}",
                    TransactionTimestamp = DateTime.UtcNow,
                    OrderId = refund.ExternalReference ?? refund.Id.ToString()
                };

                // Enviar a notificação
                await _listenerService.GenerateNotification(notification);
                _logger.LogInformation("Notificação de estorno enviada: {RefundId}, Status: {Status}, Evento: {EventType}",
                    refund.Id, refund.Status, eventType);
            }
            catch (Exception ex)
            {
                // Não queremos que falhas de notificação afetem o fluxo principal
                _logger.LogError(ex, "Erro ao enviar notificação de estorno {RefundId}", refund.Id);
                // As notificações falhas serão retentadas pelo NotificationRetryService
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
                Id = refund.Id,
                TransactionId = refund.TransactionId,
                Amount = refund.Amount,
                Status = refund.Status.ToString(),
                Reason = refund.Reason,
                ExternalReference = refund.ExternalReference,
                CreatedAt = refund.CreatedAt,
                ProcessedAt = refund.ProcessedAt,
                FailReason = refund.FailReason
            };
        }
    }
}