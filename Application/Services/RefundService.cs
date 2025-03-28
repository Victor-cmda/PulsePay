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
        private readonly IListenerService _listenerService;
        private readonly ILogger<RefundService> _logger;

        public RefundService(
            IRefundRepository refundRepository,
            ITransactionRepository transactionRepository,
            IListenerService listenerService,
            ILogger<RefundService> logger)
        {
            _refundRepository = refundRepository;
            _transactionRepository = transactionRepository;
            _listenerService = listenerService;
            _logger = logger;
        }

        public async Task<RefundResponseDto> RequestRefundAsync(RefundRequestDto request, Guid sellerId)
        {
            // Verificar se a transação existe
            var transaction = await _transactionRepository.GetByIdAsync(request.TransactionId);
            if (transaction == null)
                throw new NotFoundException($"Transação com ID {request.TransactionId} não encontrada");

            // Verificar se a transação pertence ao vendedor
            if (transaction.SellerId != sellerId)
                throw new NotFoundException($"Transação com ID {request.TransactionId} não encontrada");

            // Verificar se a transação já foi completada (só pode estornar transações completadas)
            if (transaction.Status != "COMPLETED" && transaction.Status != "APPROVED")
                throw new ValidationException("Apenas transações completadas podem ser estornadas");

            // Verificar se o valor do estorno não excede o valor da transação
            if (request.Amount > transaction.Amount)
                throw new ValidationException("O valor do estorno não pode ser maior que o valor da transação");

            // Criar o estorno
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

            // Salvar o estorno
            var result = await _refundRepository.CreateAsync(refund);
            _logger.LogInformation("Estorno criado: {RefundId} para transação {TransactionId} - Valor: {Amount}",
                result.Id, result.TransactionId, result.Amount);

            // Enviar notificação de estorno criado
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

            // Atualizar status para Processing
            refund.Status = RefundStatus.Processing;
            refund.ProcessedAt = DateTime.UtcNow;

            var result = await _refundRepository.UpdateAsync(refund);
            _logger.LogInformation("Estorno {RefundId} aprovado pelo admin {AdminId}", id, adminId);

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
            // Aqui poderíamos salvar o recibo da transação, se o modelo tiver esse campo

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