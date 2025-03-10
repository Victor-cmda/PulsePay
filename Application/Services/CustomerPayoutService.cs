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

namespace Application.Services
{
    public class CustomerPayoutService : ICustomerPayoutService
    {
        private readonly ICustomerPayoutRepository _payoutRepository;
        private readonly IPixService _pixService;
        private readonly ILogger<CustomerPayoutService> _logger;

        public CustomerPayoutService(
            ICustomerPayoutRepository payoutRepository,
            IPixService pixService,
            ILogger<CustomerPayoutService> logger)
        {
            _payoutRepository = payoutRepository;
            _pixService = pixService;
            _logger = logger;
        }

        // Valida a chave PIX
        public async Task<PixKeyValidationDto> ValidatePixKeyAsync(PixValidationRequestDto request)
        {
            try
            {
                var validationResult = await _pixService.ValidatePixKeyAsync(
                    request.PixKey,
                    request.PixKeyType);

                if (validationResult.IsValid)
                {
                    _logger.LogInformation("Chave PIX validada com sucesso: {PixKey}, Tipo: {PixKeyType}",
                        request.PixKey, request.PixKeyType);
                }
                else
                {
                    _logger.LogWarning("Validação de chave PIX falhou: {PixKey}, Erro: {ErrorMessage}",
                        request.PixKey, validationResult.ErrorMessage);
                }

                return validationResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao validar chave PIX {PixKey}", request.PixKey);
                throw new ValidationException($"Falha na validação: {ex.Message}");
            }
        }

        // Cria um pagamento baseado em uma validação
        public async Task<CustomerPayoutResponseDto> CreatePayoutAsync(CustomerPayoutCreateDto request, Guid sellerId)
        {
            // Valida se o ID de validação existe
            if (string.IsNullOrEmpty(request.ValidationId))
                throw new ValidationException("ID de validação inválido ou ausente");

            // Verifica se já existe um pagamento com esse validationId
            var existingPayout = await _payoutRepository.GetByValidationIdAsync(request.ValidationId);
            if (existingPayout != null)
                throw new ConflictException($"Já existe um pagamento para esta validação: Status {existingPayout.Status}");

            // Criando o pagamento
            var payout = new CustomerPayout
            {
                Id = Guid.NewGuid(),
                SellerId = sellerId,
                Amount = request.Amount,
                Status = CustomerPayoutStatus.Pending,
                RequestedAt = DateTime.UtcNow,
                PixKey = "Obtido na validação", // Na verdade, isso seria obtido do armazenamento da validação
                PixKeyType = "Obtido na validação", // Na verdade, isso seria obtido do armazenamento da validação
                Description = request.Description ?? "Pagamento PIX",
                ValidationId = request.ValidationId,
                ValidatedAt = DateTime.UtcNow
            };

            var result = await _payoutRepository.CreateAsync(payout);
            _logger.LogInformation("Pagamento PIX solicitado: {PayoutId}, ValidaçãoID: {ValidationId}, Valor: {Amount}",
                result.Id, result.ValidationId, result.Amount);

            return MapToResponseDto(result);
        }

        // Obtém um pagamento por ID
        public async Task<CustomerPayoutResponseDto> GetPayoutAsync(Guid id)
        {
            var payout = await _payoutRepository.GetByIdAsync(id);
            if (payout == null)
                throw new NotFoundException($"Pagamento com ID {id} não encontrado");

            return MapToResponseDto(payout);
        }

        // Lista pagamentos de um vendedor
        public async Task<IEnumerable<CustomerPayoutResponseDto>> GetPayoutsBySellerIdAsync(Guid sellerId, int page = 1, int pageSize = 20)
        {
            var payouts = await _payoutRepository.GetBySellerIdAsync(sellerId, page, pageSize);
            return payouts.Select(MapToResponseDto);
        }

        // Lista pagamentos pendentes para administradores
        public async Task<IEnumerable<CustomerPayoutResponseDto>> GetPendingPayoutsAsync(int page = 1, int pageSize = 20)
        {
            var payouts = await _payoutRepository.GetByStatusAsync(CustomerPayoutStatus.Pending, page, pageSize);
            return payouts.Select(MapToResponseDto);
        }

        // Confirma um pagamento (admin)
        public async Task<CustomerPayoutResponseDto> ConfirmPayoutAsync(Guid payoutId, string paymentProofId, string adminId)
        {
            var payout = await _payoutRepository.GetByIdAsync(payoutId);
            if (payout == null)
                throw new NotFoundException($"Pagamento com ID {payoutId} não encontrado");

            // Permitir confirmação apenas para pagamentos pendentes
            if (payout.Status != CustomerPayoutStatus.Pending)
                throw new ValidationException($"Pagamento não está pendente. Status atual: {payout.Status}");

            try
            {
                // Confirmar o pagamento PIX através do serviço de PIX
                var confirmationResult = await _pixService.ConfirmPixPaymentAsync(
                    payout.ValidationId,
                    payout.Amount);

                if (confirmationResult.Success)
                {
                    // Atualizar o pagamento
                    payout.Status = CustomerPayoutStatus.Completed;
                    payout.ProcessedAt = DateTime.UtcNow;
                    payout.ConfirmedByAdminId = adminId;
                    payout.ConfirmedAt = DateTime.UtcNow;
                    payout.PaymentProofId = paymentProofId;
                    payout.PaymentId = confirmationResult.PaymentId;

                    await _payoutRepository.UpdateAsync(payout);

                    _logger.LogInformation("Pagamento PIX confirmado: {PayoutId}, Valor: {Amount}, Admin: {AdminId}",
                        payoutId, payout.Amount, adminId);
                }
                else
                {
                    // Atualizar o pagamento como falha
                    payout.Status = CustomerPayoutStatus.Failed;
                    payout.RejectionReason = confirmationResult.ErrorMessage;
                    payout.ProcessedAt = DateTime.UtcNow;

                    await _payoutRepository.UpdateAsync(payout);

                    _logger.LogWarning("Confirmação de pagamento PIX falhou: {PayoutId}, Erro: {ErrorMessage}",
                        payoutId, confirmationResult.ErrorMessage);

                    throw new ValidationException($"Falha na confirmação do pagamento: {confirmationResult.ErrorMessage}");
                }

                return MapToResponseDto(payout);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao confirmar pagamento PIX para {PayoutId}", payoutId);
                throw new ValidationException($"Erro ao processar confirmação: {ex.Message}");
            }
        }

        // Rejeita um pagamento (admin)
        public async Task<CustomerPayoutResponseDto> RejectPayoutAsync(Guid payoutId, string reason, string adminId)
        {
            var payout = await _payoutRepository.GetByIdAsync(payoutId);
            if (payout == null)
                throw new NotFoundException($"Pagamento com ID {payoutId} não encontrado");

            if (payout.Status != CustomerPayoutStatus.Pending)
                throw new ValidationException($"Pagamento não está pendente. Status atual: {payout.Status}");

            payout.Status = CustomerPayoutStatus.Rejected;
            payout.RejectionReason = reason;
            payout.ProcessedAt = DateTime.UtcNow;
            payout.ConfirmedByAdminId = adminId;
            payout.ConfirmedAt = DateTime.UtcNow;

            await _payoutRepository.UpdateAsync(payout);

            _logger.LogInformation("Pagamento rejeitado: {PayoutId}, Motivo: {Reason}, Admin: {AdminId}",
                payoutId, reason, adminId);

            return MapToResponseDto(payout);
        }

        // Mapeia o modelo para DTO de resposta
        private CustomerPayoutResponseDto MapToResponseDto(CustomerPayout payout)
        {
            return new CustomerPayoutResponseDto
            {
                Id = payout.Id,
                Amount = payout.Amount,
                Status = payout.Status.ToString(),
                RequestedAt = payout.RequestedAt,
                ProcessedAt = payout.ProcessedAt,
                PixKey = payout.PixKey,
                PixKeyType = payout.PixKeyType,
                ValidationId = payout.ValidationId,
                PaymentId = payout.PaymentId
            };
        }
    }
}