using Application.DTOs;
using Application.DTOs.Pix;
using Application.Interfaces;
using Domain.Interfaces;
using Domain.Models;
using Microsoft.Extensions.Logging;
using Shared.Enums;
using Shared.Exceptions;

namespace Application.Services
{
    public class DepositService : IDepositService
    {
        private readonly IDepositRepository _depositRepository;
        private readonly IWalletService _walletService;
        private readonly IPaymentService _paymentService;
        private readonly IWalletRepository _walletRepository;
        private readonly ILogger<DepositService> _logger;

        public DepositService(
            IDepositRepository depositRepository,
            IWalletService walletService,
            IPaymentService paymentService,
            IWalletRepository walletRepository,
            ILogger<DepositService> logger)
        {
            _depositRepository = depositRepository;
            _walletService = walletService;
            _walletRepository = walletRepository;
            _paymentService = paymentService;
            _logger = logger;
        }

        public async Task<DepositDto> CreateDepositRequestAsync(DepositRequestDto request)
        {
            var wallets = await _walletRepository.GetAllBySellerIdAsync(request.SellerId);

            var depositWallet = wallets.FirstOrDefault(w => w.WalletType == WalletType.Deposit);
            var generalWallet = wallets.FirstOrDefault(w => w.WalletType == WalletType.General);

            Wallet specificWallet = null;
            if (request.WalletId != Guid.Empty)
            {
                specificWallet = await _walletRepository.GetByIdAsync(request.WalletId);
                if (specificWallet != null && specificWallet.SellerId != request.SellerId)
                {
                    specificWallet = null;
                }
                else if (specificWallet != null && specificWallet.WalletType == WalletType.Withdrawal)
                {
                    throw new ValidationException("Não é possível criar um depósito para uma carteira de Saque (Withdrawal). Use uma carteira de Depósito (Deposit) ou Geral (General).");
                }
            }

            var targetWallet = specificWallet ?? depositWallet ?? generalWallet;

            if (targetWallet == null)
            {
                throw new ValidationException("Não foi encontrada uma carteira de Depósito (Deposit) ou Geral (General) para processar o depósito");
            }

            var pixRequest = new PaymentPixRequestDto
            {
                Amount = request.Amount,
                OrderId = Guid.NewGuid().ToString(),
                CustomerId = request.SellerId.ToString(),
                Name = $"PULSEPAY",
                Email = "admin@pulseauth.com",
                Document = "00000000000",
                DocumentType = "CPF"
            };

            var pixResponse = await _paymentService.GeneratePixPayment(pixRequest, request.SellerId);

            var deposit = new Deposit
            {
                Id = Guid.NewGuid(),
                SellerId = request.SellerId,
                WalletId = targetWallet.Id,
                Amount = request.Amount,
                Status = DepositStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                TransactionId = pixResponse.TransactionId,
                QrCode = pixResponse.QrCode,
                Notes = $"CustomerId {pixRequest.CustomerId}",
                ExternalReference = pixResponse.OrderId,
                PaymentProvider = "GetNet",
                ReceiptId = $"OrderId {pixRequest.OrderId}",
                PaymentMethod = "PIX"
            };

            var result = await _depositRepository.CreateAsync(deposit);
            _logger.LogInformation("Solicitação de depósito criada: {DepositId} para vendedor {SellerId} - Valor: {Amount} - Carteira: {WalletType}",
                result.Id, result.SellerId, result.Amount, targetWallet.WalletType);

            return MapToDto(result);
        }

        public async Task<DepositDto> GetDepositAsync(Guid id)
        {
            var deposit = await _depositRepository.GetByIdAsync(id);
            if (deposit == null)
                throw new NotFoundException($"Depósito com ID {id} não encontrado");

            return MapToDto(deposit);
        }

        public async Task<IEnumerable<DepositDto>> GetDepositsBySellerIdAsync(Guid sellerId, int page = 1, int pageSize = 20)
        {
            var deposits = await _depositRepository.GetBySellerIdAsync(sellerId, page, pageSize);
            return deposits.Select(MapToDto);
        }

        public async Task<DepositDto> ProcessDepositCallbackAsync(string transactionId, string status, decimal amount)
        {
            var deposit = await _depositRepository.GetByTransactionIdAsync(transactionId);
            if (deposit == null)
                throw new NotFoundException($"Depósito com ID de transação {transactionId} não encontrado");

            if (deposit.Status != DepositStatus.Pending)
            {
                _logger.LogWarning("Tentativa de processar depósito {DepositId} com status {Status}",
                    deposit.Id, deposit.Status);
                return MapToDto(deposit);
            }

            if (deposit.Amount != amount)
            {
                _logger.LogWarning("Valor do depósito {DepositId} não corresponde. Esperado: {Expected}, Recebido: {Received}",
                    deposit.Id, deposit.Amount, amount);

                throw new ValidationException($"Valor do pagamento não corresponde ao valor do depósito");
            }

            if (status.ToLower() == "approved" || status.ToLower() == "completed")
            {
                deposit.Status = DepositStatus.Completed;
                deposit.ProcessedAt = DateTime.UtcNow;

                await _depositRepository.UpdateAsync(deposit);

                var wallets = await _walletRepository.GetAllBySellerIdAsync(deposit.SellerId);

                var depositWallet = wallets.FirstOrDefault(w => w.WalletType == WalletType.Deposit);

                var generalWallet = wallets.FirstOrDefault(w => w.WalletType == WalletType.General);

                var targetWallet = depositWallet ?? generalWallet ?? deposit.Wallet;

                if (targetWallet == null)
                {
                    _logger.LogError("Não foi encontrada carteira apropriada para depósito {DepositId}", deposit.Id);
                    throw new NotFoundException($"Carteira de depósito não encontrada para o vendedor {deposit.SellerId}");
                }

                await _walletService.AddFundsAsync(targetWallet.Id, new WalletOperationDto
                {
                    Amount = deposit.Amount,
                    Description = $"Depósito via {deposit.PaymentMethod}",
                    Reference = $"DEPOSIT_{deposit.Id}"
                });

                _logger.LogInformation("Depósito {DepositId} processado com sucesso. Valor: {Amount} adicionado à carteira {WalletType}",
                    deposit.Id, deposit.Amount, targetWallet.WalletType);
            }
            else if (status.ToLower() == "failed" || status.ToLower() == "cancelled")
            {
                deposit.Status = DepositStatus.Failed;
                deposit.ProcessedAt = DateTime.UtcNow;

                await _depositRepository.UpdateAsync(deposit);

                _logger.LogInformation("Depósito {DepositId} falhou. Status: {Status}",
                    deposit.Id, status);
            }
            else
            {
                _logger.LogWarning("Status desconhecido para depósito {DepositId}: {Status}",
                    deposit.Id, status);
            }

            return MapToDto(deposit);
        }

        private DepositDto MapToDto(Deposit deposit)
        {
            return new DepositDto
            {
                Id = deposit.Id,
                SellerId = deposit.SellerId,
                WalletId = deposit.WalletId,
                Amount = deposit.Amount,
                Status = deposit.Status.ToString(),
                CreatedAt = deposit.CreatedAt,
                ExpiresAt = deposit.ExpiresAt,
                ProcessedAt = deposit.ProcessedAt,
                TransactionId = deposit.TransactionId,
                QrCode = deposit.QrCode,
                PaymentMethod = deposit.PaymentMethod
            };
        }
    }
}
