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
            if (string.IsNullOrEmpty(request.SellerName) ||
                string.IsNullOrEmpty(request.SellerEmail) ||
                string.IsNullOrEmpty(request.SellerDocument) ||
                string.IsNullOrEmpty(request.SellerDocumentType))
            {
                throw new ValidationException("Informações do vendedor são obrigatórias: Nome, Email, Documento e Tipo de Documento");
            }

            // Encontrar a carteira correta para o depósito (deve ser Deposit ou General)
            var wallets = await _walletRepository.GetAllBySellerIdAsync(request.SellerId);

            // Primeiro tenta encontrar uma carteira Deposit
            var depositWallet = wallets.FirstOrDefault(w => w.WalletType == WalletType.Deposit);

            // Se não encontrar, tenta usar uma carteira General
            var generalWallet = wallets.FirstOrDefault(w => w.WalletType == WalletType.General);

            // Se uma carteira específica foi solicitada, verifica se ela é adequada
            Wallet specificWallet = null;
            if (request.WalletId != Guid.Empty)
            {
                specificWallet = await _walletRepository.GetByIdAsync(request.WalletId);
                if (specificWallet != null && specificWallet.SellerId != request.SellerId)
                {
                    specificWallet = null; // A carteira não pertence a este vendedor
                }
                else if (specificWallet != null && specificWallet.WalletType == WalletType.Withdrawal)
                {
                    throw new ValidationException("Não é possível criar um depósito para uma carteira de Saque (Withdrawal). Use uma carteira de Depósito (Deposit) ou Geral (General).");
                }
            }

            // Determina qual carteira usar para o depósito
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
                Name = request.SellerName,
                Email = request.SellerEmail,
                Document = request.SellerDocument,
                DocumentType = request.SellerDocumentType
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

        // Em Application/Services/DepositService.cs (continuação)

        public async Task<DepositDto> ProcessDepositCallbackAsync(string transactionId, string status, decimal amount)
        {
            var deposit = await _depositRepository.GetByTransactionIdAsync(transactionId);
            if (deposit == null)
                throw new NotFoundException($"Depósito com ID de transação {transactionId} não encontrado");

            // Check if the deposit has already been processed
            if (deposit.Status != DepositStatus.Pending)
            {
                _logger.LogWarning("Tentativa de processar depósito {DepositId} com status {Status}",
                    deposit.Id, deposit.Status);
                return MapToDto(deposit);
            }

            // Check if the amount matches
            if (deposit.Amount != amount)
            {
                _logger.LogWarning("Valor do depósito {DepositId} não corresponde. Esperado: {Expected}, Recebido: {Received}",
                    deposit.Id, deposit.Amount, amount);

                throw new ValidationException($"Valor do pagamento não corresponde ao valor do depósito");
            }

            // Process the payment status
            if (status.ToLower() == "approved" || status.ToLower() == "completed")
            {
                // Update the deposit
                deposit.Status = DepositStatus.Completed;
                deposit.ProcessedAt = DateTime.UtcNow;

                await _depositRepository.UpdateAsync(deposit);

                // Find the correct wallet to add funds to
                var wallets = await _walletRepository.GetAllBySellerIdAsync(deposit.SellerId);

                // First try to find a Deposit wallet
                var depositWallet = wallets.FirstOrDefault(w => w.WalletType == WalletType.Deposit);

                // If no Deposit wallet exists, try to use a General wallet
                var generalWallet = wallets.FirstOrDefault(w => w.WalletType == WalletType.General);

                // Determine which wallet to use for the deposit
                var targetWallet = depositWallet ?? generalWallet ?? deposit.Wallet;

                if (targetWallet == null)
                {
                    _logger.LogError("Não foi encontrada carteira apropriada para depósito {DepositId}", deposit.Id);
                    throw new NotFoundException($"Carteira de depósito não encontrada para o vendedor {deposit.SellerId}");
                }

                // Add funds to the wallet
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
                // Update the deposit as failed
                deposit.Status = DepositStatus.Failed;
                deposit.ProcessedAt = DateTime.UtcNow;

                await _depositRepository.UpdateAsync(deposit);

                _logger.LogInformation("Depósito {DepositId} falhou. Status: {Status}",
                    deposit.Id, status);
            }
            else
            {
                // Unknown status
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
