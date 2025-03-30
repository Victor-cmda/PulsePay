using Application.DTOs;
using Application.Interfaces;
using Domain.Interfaces;
using Domain.Models;
using Microsoft.Extensions.Logging;
using Shared.Enums;
using Shared.Exceptions;

namespace Application.Services
{
    public class CustomerPayoutService : ICustomerPayoutService
    {
        private readonly ICustomerPayoutRepository _payoutRepository;
        private readonly IWalletRepository _walletRepository;
        private readonly IWalletService _walletService;
        private readonly IPixService _pixService;
        private readonly IPixValidationCacheService _validationCacheService;
        private readonly ILogger<CustomerPayoutService> _logger;
        private readonly IListenerService _listenerService;
        public CustomerPayoutService(
            ICustomerPayoutRepository payoutRepository,
            IWalletRepository walletRepository,
            IWalletService walletService,
            IPixService pixService,
            IPixValidationCacheService validationCacheService,
            IListenerService listenerService,
            ILogger<CustomerPayoutService> logger)
        {
            _payoutRepository = payoutRepository;
            _walletRepository = walletRepository;
            _walletService = walletService;
            _pixService = pixService;
            _listenerService = listenerService;
            _validationCacheService = validationCacheService;
            _logger = logger;
        }

        public async Task<PixKeyValidationDto> ValidatePixKeyAsync(PixValidationRequestDto request)
        {
            try
            {
                var validationResult = await _pixService.ValidatePixKeyAsync(
                    request.PixKey,
                    request.PixKeyType);

                if (validationResult.IsValid)
                {
                    await _validationCacheService.StoreValidationAsync(validationResult);

                    _logger.LogInformation("Chave PIX validada com sucesso: {PixKey}, Tipo: {PixKeyType}, ValidationId: {ValidationId}",
                        request.PixKey, request.PixKeyType, validationResult.ValidationId);
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

        public async Task<CustomerPayoutResponseDto> CreatePayoutAsync(CustomerPayoutCreateDto request, Guid sellerId)
        {
            if (string.IsNullOrEmpty(request.ValidationId))
                throw new ValidationException("ID de validação inválido ou ausente");

            var existingPayout = await _payoutRepository.GetByValidationIdAsync(request.ValidationId);
            if (existingPayout != null)
                throw new ConflictException($"Já existe um pagamento para esta validação: Status {existingPayout.Status}");

            var validationData = await _validationCacheService.GetValidationAsync(request.ValidationId);
            if (validationData == null)
                throw new ValidationException($"Validação com ID {request.ValidationId} não encontrada ou expirada. Por favor, realize a validação novamente.");

            if (!validationData.IsValid)
                throw new ValidationException($"A validação indicada não é válida. Motivo: {validationData.ErrorMessage}");

            var wallets = await _walletRepository.GetAllBySellerIdAsync(sellerId);

            var withdrawalWallet = wallets.FirstOrDefault(w => w.WalletType == WalletType.Withdrawal);

            var generalWallet = wallets.FirstOrDefault(w => w.WalletType == WalletType.General);

            var payoutWallet = withdrawalWallet ?? generalWallet;

            if (payoutWallet == null)
            {
                throw new ValidationException("Não foi encontrada uma carteira de Saque (Withdrawal) ou Geral (General) para processar o pagamento");
            }

            var hasBalance = await _walletService.HasSufficientFundsAsync(payoutWallet.Id, request.Amount);
            if (!hasBalance)
            {
                throw new InsufficientFundsException($"Saldo insuficiente na carteira de {payoutWallet.WalletType} para processar o pagamento");
            }

            await _walletService.DeductFundsAsync(payoutWallet.Id, new WalletOperationDto
            {
                Amount = request.Amount,
                Description = $"Pagamento PIX pendente - {request.ValidationId}",
                Reference = $"PIX_PAYMENT_{request.ValidationId}"
            });

            var payout = new CustomerPayout
            {
                Id = Guid.NewGuid(),
                SellerId = sellerId,
                Amount = request.Amount,
                Status = CustomerPayoutStatus.Pending,
                RequestedAt = DateTime.UtcNow,
                PixKey = validationData.keyValue,
                PixKeyType = validationData.keyType,
                Description = request.Description ?? "Pagamento PIX",
                ValidationId = request.ValidationId,
                ValidatedAt = DateTime.UtcNow,
                WalletId = payoutWallet.Id
            };


            var result = await _payoutRepository.CreateAsync(payout);

            await SendPayoutNotification(payout);

            await _validationCacheService.RemoveValidationAsync(request.ValidationId);

            _logger.LogInformation("Pagamento PIX solicitado: {PayoutId}, ValidaçãoID: {ValidationId}, Valor: {Amount}, Carteira: {WalletType}",
                result.Id, result.ValidationId, result.Amount, payoutWallet.WalletType);

            return MapToResponseDto(result);
        }

        public async Task<CustomerPayoutResponseDto> GetPayoutAsync(Guid id)
        {
            var payout = await _payoutRepository.GetByIdAsync(id);
            if (payout == null)
                throw new NotFoundException($"Pagamento com ID {id} não encontrado");

            return MapToResponseDto(payout);
        }

        public async Task<IEnumerable<CustomerPayoutResponseDto>> GetPayoutsBySellerIdAsync(Guid sellerId, int page = 1, int pageSize = 20)
        {
            var payouts = await _payoutRepository.GetBySellerIdAsync(sellerId, page, pageSize);
            return payouts.Select(MapToResponseDto);
        }

        public async Task<IEnumerable<CustomerPayoutResponseDto>> GetPendingPayoutsAsync(int page = 1, int pageSize = 20)
        {
            var payouts = await _payoutRepository.GetByStatusAsync(CustomerPayoutStatus.Pending, page, pageSize);
            return payouts.Select(MapToResponseDto);
        }

        public async Task<CustomerPayoutResponseDto> ConfirmPayoutAsync(Guid payoutId, string paymentProofId, string adminId)
        {
            var payout = await _payoutRepository.GetByIdAsync(payoutId);
            if (payout == null)
                throw new NotFoundException($"Pagamento com ID {payoutId} não encontrado");

            if (payout.Status != CustomerPayoutStatus.Pending)
                throw new ValidationException($"Pagamento não está pendente. Status atual: {payout.Status}");

            try
            {
                var confirmationResult = await _pixService.ConfirmPixPaymentAsync(
                    payout.ValidationId,
                    payout.Amount);

                if (confirmationResult.Success)
                {
                    if (payout.WalletId != Guid.Empty)
                    {
                        var wallet = await _walletRepository.GetByIdAsync(payout.WalletId);
                        if (wallet == null)
                        {
                            _logger.LogWarning("A carteira {WalletId} associada ao pagamento não foi encontrada", payout.WalletId);

                            // Podemos tentar encontrar outra carteira adequada, mas o saldo já foi deduzido
                            // neste ponto, então não precisamos deduzir novamente
                        }
                        else if (wallet.WalletType == WalletType.Deposit)
                        {
                            _logger.LogWarning("Pagamento foi incorretamente deduzido de uma carteira de Depósito");

                            // Neste caso, precisamos corrigir - estornar para a carteira de Depósito
                            // e deduzir da carteira de Saque ou Geral
                            var wallets = await _walletRepository.GetAllBySellerIdAsync(payout.SellerId);
                            var withdrawalWallet = wallets.FirstOrDefault(w => w.WalletType == WalletType.Withdrawal);
                            var generalWallet = wallets.FirstOrDefault(w => w.WalletType == WalletType.General);
                            var correctWallet = withdrawalWallet ?? generalWallet;

                            if (correctWallet != null)
                            {
                                await using var dbTransaction = await _walletRepository.BeginTransactionAsync();
                                try
                                {
                                    await _walletService.AddFundsAsync(wallet.Id, new WalletOperationDto
                                    {
                                        Amount = payout.Amount,
                                        Description = "Correção - Estorno de pagamento da carteira de Depósito",
                                        Reference = $"CORRECTION_DEPOSIT_{payout.Id}"
                                    });

                                    await _walletService.DeductFundsAsync(correctWallet.Id, new WalletOperationDto
                                    {
                                        Amount = payout.Amount,
                                        Description = "Correção - Pagamento PIX deduzido da carteira adequada",
                                        Reference = $"CORRECTION_WITHDRAW_{payout.Id}"
                                    });

                                    await dbTransaction.CommitAsync();

                                    payout.WalletId = correctWallet.Id;

                                    _logger.LogInformation("Correção aplicada: pagamento movido da carteira de Depósito para {WalletType}",
                                        correctWallet.WalletType);
                                }
                                catch (Exception ex)
                                {
                                    await dbTransaction.RollbackAsync();
                                    throw new ValidationException($"Erro ao corrigir carteira associada ao pagamento: {ex.Message}");
                                }
                            }
                        }
                    }

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

                await SendPayoutNotification(payout);

                return MapToResponseDto(payout);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao confirmar pagamento PIX para {PayoutId}", payoutId);
                throw new ValidationException($"Erro ao processar confirmação: {ex.Message}");
            }
        }

        public async Task<CustomerPayoutResponseDto> RejectPayoutAsync(Guid payoutId, string reason, string adminId)
        {
            var payout = await _payoutRepository.GetByIdAsync(payoutId);
            if (payout == null)
                throw new NotFoundException($"Pagamento com ID {payoutId} não encontrado");

            if (payout.Status != CustomerPayoutStatus.Pending)
                throw new ValidationException($"Pagamento não está pendente. Status atual: {payout.Status}");

            // Iniciar transação para garantir consistência
            await using var dbTransaction = await _walletRepository.BeginTransactionAsync();

            try
            {
                // Se o pagamento foi deduzido de uma carteira específica, restaurar os fundos
                if (payout.WalletId != Guid.Empty)
                {
                    var wallet = await _walletRepository.GetByIdAsync(payout.WalletId);
                    if (wallet != null)
                    {
                        // Restaurar os fundos para a mesma carteira
                        await _walletService.AddFundsAsync(wallet.Id, new WalletOperationDto
                        {
                            Amount = payout.Amount,
                            Description = $"Estorno de pagamento PIX rejeitado - Motivo: {reason}",
                            Reference = $"PIX_PAYMENT_REJECTED_{payout.Id}"
                        });

                        _logger.LogInformation("Fundos restaurados para carteira {WalletType} após rejeição de pagamento PIX",
                            wallet.WalletType);
                    }
                    else
                    {
                        _logger.LogWarning("Não foi possível encontrar a carteira {WalletId} para restaurar fundos do pagamento rejeitado",
                            payout.WalletId);

                        // Tentar encontrar uma carteira adequada para restaurar os fundos
                        var wallets = await _walletRepository.GetAllBySellerIdAsync(payout.SellerId);

                        // A ordem de preferência para restaurar fundos é: Withdrawal > General
                        var withdrawalWallet = wallets.FirstOrDefault(w => w.WalletType == WalletType.Withdrawal);
                        var generalWallet = wallets.FirstOrDefault(w => w.WalletType == WalletType.General);
                        var targetWallet = withdrawalWallet ?? generalWallet;

                        if (targetWallet != null)
                        {
                            await _walletService.AddFundsAsync(targetWallet.Id, new WalletOperationDto
                            {
                                Amount = payout.Amount,
                                Description = $"Estorno de pagamento PIX rejeitado - Motivo: {reason}",
                                Reference = $"PIX_PAYMENT_REJECTED_{payout.Id}"
                            });

                            _logger.LogInformation("Fundos restaurados para carteira alternativa {WalletType} após rejeição de pagamento PIX",
                                targetWallet.WalletType);
                        }
                        else
                        {
                            throw new ValidationException("Não foi possível encontrar uma carteira adequada para restaurar os fundos do pagamento rejeitado");
                        }
                    }
                }

                payout.Status = CustomerPayoutStatus.Rejected;
                payout.RejectionReason = reason;
                payout.ProcessedAt = DateTime.UtcNow;
                payout.ConfirmedByAdminId = adminId;
                payout.ConfirmedAt = DateTime.UtcNow;

                await _payoutRepository.UpdateAsync(payout);

                await dbTransaction.CommitAsync();

                _logger.LogInformation("Pagamento rejeitado: {PayoutId}, Motivo: {Reason}, Admin: {AdminId}",
                    payoutId, reason, adminId);

                await SendPayoutNotification(payout);

                return MapToResponseDto(payout);
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();
                _logger.LogError(ex, "Erro ao rejeitar pagamento PIX {PayoutId}", payoutId);
                throw;
            }
        }

        private async Task SendPayoutNotification(CustomerPayout payout)
        {
            try
            {
                var notification = NotificationHelpers.CreatePayoutNotification(payout);
                await _listenerService.GenerateNotification(notification);
                _logger.LogInformation("Payout notification sent: {PayoutId}, Status: {Status}",
                    payout.Id, payout.Status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending payout notification {PayoutId}", payout.Id);
            }
        }

        private CustomerPayoutResponseDto MapToResponseDto(CustomerPayout payout)
        {
            return new CustomerPayoutResponseDto
            {
                Id = payout.Id,
                Amount = payout.Amount,
                Description = payout.Description,
                Status = payout.Status.ToString(),
                RequestedAt = payout.RequestedAt,
                ProcessedAt = payout.ProcessedAt,
                PixKeyType = payout.PixKeyType,
                PixKeyValue = payout.PixKey,
                ValidationId = payout.ValidationId,
                PaymentId = payout.PaymentId
            };
        }
    }
}