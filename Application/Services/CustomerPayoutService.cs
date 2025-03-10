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
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class CustomerPayoutService : ICustomerPayoutService
    {
        private readonly ICustomerPayoutRepository _payoutRepository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly IPixService _pixService;
        private readonly ILogger<CustomerPayoutService> _logger;

        public CustomerPayoutService(
            ICustomerPayoutRepository payoutRepository,
            ITransactionRepository transactionRepository,
            IPixService pixService,
            ILogger<CustomerPayoutService> logger)
        {
            _payoutRepository = payoutRepository;
            _transactionRepository = transactionRepository;
            _pixService = pixService;
            _logger = logger;
        }

        public async Task<CustomerPayoutDto> RequestPayoutAsync(CustomerPayoutRequestDto request)
        {
            var transaction = await _transactionRepository.GetByIdAsync(request.TransactionId);
            if (transaction == null)
                throw new NotFoundException($"Transação com ID {request.TransactionId} não encontrada");

            if (transaction.SellerId != request.SellerId)
                throw new UnauthorizedException("A transação não pertence a este vendedor");

            var existingPayout = await _payoutRepository.GetByTransactionIdAsync(request.TransactionId);
            if (existingPayout != null)
                throw new ConflictException($"Já existe um pagamento solicitado para esta transação: Status {existingPayout.Status}");

            if (request.Amount <= 0 || request.Amount > transaction.Amount)
                throw new ValidationException($"Valor inválido. O valor deve ser maior que zero e menor ou igual ao valor da transação ({transaction.Amount})");

            var pixValidation = await _pixService.ValidatePixKeyAsync(request.PixKey, request.PixKeyType);
            if (!pixValidation.IsValid)
            {
                throw new ValidationException($"Chave PIX inválida: {pixValidation.ErrorMessage}");
            }

            // Criar o pagamento com a chave já validada
            var payout = new CustomerPayout
            {
                Id = Guid.NewGuid(),
                SellerId = request.SellerId,
                TransactionId = request.TransactionId,
                Amount = request.Amount,
                Status = CustomerPayoutStatus.Validated, 
                RequestedAt = DateTime.UtcNow,
                CustomerName = request.CustomerName,
                CustomerEmail = request.CustomerEmail,
                CustomerDocument = request.CustomerDocument,
                CustomerDocumentType = request.CustomerDocumentType,
                PixKey = request.PixKey,
                PixKeyType = request.PixKeyType,
                Description = request.Description ?? "Pagamento ao cliente",
                IsPixKeyValidated = true,
                PixInfoValidated = System.Text.Json.JsonSerializer.Serialize(pixValidation),
                ValidatedAt = DateTime.UtcNow,
                ValidatedBy = "System"
            };

            var result = await _payoutRepository.CreateAsync(payout);
            _logger.LogInformation("Pagamento ao cliente solicitado e validado: {PayoutId} para transação {TransactionId} - Valor: {Amount}",
                result.Id, result.TransactionId, result.Amount);

            return MapToDto(result);
        }

        public async Task<CustomerPayoutDto> GetPayoutAsync(Guid id, Guid sellerId)
        {
            var payout = await _payoutRepository.GetByIdAsync(id);
            if (payout == null)
                throw new NotFoundException($"Pagamento com ID {id} não encontrado");

            // Verificar se o pagamento pertence ao vendedor solicitante
            if (payout.SellerId != sellerId)
                throw new UnauthorizedException("Você não tem permissão para acessar este pagamento");

            return MapToDto(payout);
        }

        public async Task<IEnumerable<CustomerPayoutDto>> GetPayoutsBySellerIdAsync(Guid sellerId, int page = 1, int pageSize = 20)
        {
            var payouts = await _payoutRepository.GetBySellerIdAsync(sellerId, page, pageSize);
            return payouts.Select(MapToDto);
        }

        public async Task<IEnumerable<CustomerPayoutDto>> GetPendingPayoutsAsync(int page = 1, int pageSize = 20)
        {
            var payouts = await _payoutRepository.GetByStatusAsync(CustomerPayoutStatus.Pending, page, pageSize);
            return payouts.Select(MapToDto);
        }

        public async Task<PixKeyValidationDto> ValidatePixKeyAsync(Guid payoutId)
        {
            var payout = await _payoutRepository.GetByIdAsync(payoutId);
            if (payout == null)
                throw new NotFoundException($"Pagamento com ID {payoutId} não encontrado");

            if (payout.Status != CustomerPayoutStatus.Pending)
                throw new ValidationException($"Pagamento não está no status pendente. Status atual: {payout.Status}");

            try
            {
                // Chamada para o serviço externo de validação da chave PIX
                var validationResult = await _pixService.ValidatePixKeyAsync(payout.PixKey, payout.Id.ToString());

                if (validationResult.IsValid)
                {
                    // Atualizar o pagamento com as informações validadas
                    payout.IsPixKeyValidated = true;
                    payout.PixInfoValidated = System.Text.Json.JsonSerializer.Serialize(validationResult);
                    payout.ValidatedAt = DateTime.UtcNow;
                    payout.ValidatedBy = "System";
                    payout.Status = CustomerPayoutStatus.Validated;

                    await _payoutRepository.UpdateAsync(payout);

                    _logger.LogInformation("Chave PIX validada com sucesso: {PayoutId}, Chave: {PixKey}, Tipo: {PixKeyType}",
                        payoutId, payout.PixKey, payout.PixKeyType);
                }
                else
                {
                    _logger.LogWarning("Validação de chave PIX falhou: {PayoutId}, Chave: {PixKey}, Erro: {ErrorMessage}",
                        payoutId, payout.PixKey, validationResult.ErrorMessage);
                }

                return validationResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao validar chave PIX {PixKey} para pagamento {PayoutId}",
                    payout.PixKey, payoutId);

                // Retornar falha na validação
                return new PixKeyValidationDto
                {
                    IsValid = false,
                    PixKey = payout.PixKey,
                    PixKeyType = payout.PixKeyType,
                    ErrorMessage = $"Falha na validação: {ex.Message}"
                };
            }
        }

        public async Task<CustomerPayoutDto> ConfirmPayoutAsync(Guid payoutId, decimal value, string proofReference, string adminId)
        {
            var payout = await _payoutRepository.GetByIdAsync(payoutId);
            if (payout == null)
                throw new NotFoundException($"Pagamento com ID {payoutId} não encontrado");

            // Permitir confirmação apenas para pagamentos validados
            if (payout.Status != CustomerPayoutStatus.Validated)
                throw new ValidationException($"Pagamento não está validado. Status atual: {payout.Status}");

            if (value <= 0 || value != payout.Amount)
                throw new ValidationException($"Valor inválido. O valor deve ser igual ao valor do pagamento ({payout.Amount})");

            try
            {
                // Registrar o pagamento manual com a referência do comprovante
                var confirmationResult = await _pixService.RegisterManualPaymentAsync(
                    payoutId, value, proofReference, adminId);

                if (confirmationResult.Success)
                {
                    // Atualizar o pagamento
                    payout.Status = CustomerPayoutStatus.Completed;
                    payout.ProcessedAt = DateTime.UtcNow;
                    payout.ConfirmedBy = adminId;
                    payout.ConfirmedAt = DateTime.UtcNow;
                    payout.PaymentProofId = confirmationResult.PaymentProofId;
                    payout.PaymentId = confirmationResult.PaymentId;

                    await _payoutRepository.UpdateAsync(payout);

                    _logger.LogInformation("Pagamento PIX manual confirmado: {PayoutId}, Valor: {Amount}, Comprovante: {Proof}, Admin: {AdminId}",
                        payoutId, value, proofReference, adminId);
                }
                else
                {
                    // Atualizar o pagamento como falha
                    payout.Status = CustomerPayoutStatus.Failed;
                    payout.RejectionReason = confirmationResult.ErrorMessage;
                    payout.ProcessedAt = DateTime.UtcNow;

                    await _payoutRepository.UpdateAsync(payout);

                    _logger.LogWarning("Confirmação de pagamento PIX manual falhou: {PayoutId}, Erro: {ErrorMessage}",
                        payoutId, confirmationResult.ErrorMessage);

                    throw new ValidationException($"Falha na confirmação do pagamento: {confirmationResult.ErrorMessage}");
                }

                return MapToDto(payout);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao confirmar pagamento PIX manual para {PayoutId}", payoutId);
                throw new ValidationException($"Erro ao processar confirmação: {ex.Message}");
            }
        }

        public async Task<CustomerPayoutDto> RejectPayoutAsync(Guid payoutId, string reason, string adminId)
        {
            var payout = await _payoutRepository.GetByIdAsync(payoutId);
            if (payout == null)
                throw new NotFoundException($"Pagamento com ID {payoutId} não encontrado");

            if (payout.Status != CustomerPayoutStatus.Pending && payout.Status != CustomerPayoutStatus.Validated)
                throw new ValidationException($"Pagamento não está em um status que permite rejeição. Status atual: {payout.Status}");

            payout.Status = CustomerPayoutStatus.Rejected;
            payout.RejectionReason = reason;
            payout.ProcessedAt = DateTime.UtcNow;
            payout.ConfirmedBy = adminId;
            payout.ConfirmedAt = DateTime.UtcNow;

            await _payoutRepository.UpdateAsync(payout);

            _logger.LogInformation("Pagamento rejeitado: {PayoutId}, Motivo: {Reason}, Admin: {AdminId}",
                payoutId, reason, adminId);

            return MapToDto(payout);
        }

        private CustomerPayoutDto MapToDto(CustomerPayout payout)
        {
            return new CustomerPayoutDto
            {
                Id = payout.Id,
                SellerId = payout.SellerId,
                TransactionId = payout.TransactionId,
                Amount = payout.Amount,
                Status = payout.Status.ToString(),
                RequestedAt = payout.RequestedAt,
                ProcessedAt = payout.ProcessedAt,
                CustomerName = payout.CustomerName,
                CustomerEmail = payout.CustomerEmail,
                CustomerDocument = payout.CustomerDocument,
                CustomerDocumentType = payout.CustomerDocumentType,
                PixKey = payout.PixKey,
                PixKeyType = payout.PixKeyType,
                Description = payout.Description,
                RejectionReason = payout.RejectionReason,
                PixInfoValidated = payout.PixInfoValidated,
                ValidatedAt = payout.ValidatedAt,
                ValidatedBy = payout.ValidatedBy,
                ConfirmedBy = payout.ConfirmedBy,
                ConfirmedAt = payout.ConfirmedAt,
                PaymentProofId = payout.PaymentProofId
            };
        }
    }
}
