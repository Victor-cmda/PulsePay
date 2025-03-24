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
        private readonly ILogger<DepositService> _logger;

        public DepositService(
            IDepositRepository depositRepository,
            IWalletService walletService,
            IPaymentService paymentService,
            ILogger<DepositService> logger)
        {
            _depositRepository = depositRepository;
            _walletService = walletService;
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
                WalletId = request.WalletId,
                Amount = request.Amount,
                Status = DepositStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(24), 
                TransactionId = pixResponse.TransactionId,
                QrCode = pixResponse.QrCode,
                PaymentMethod = "PIX"
            };

            var result = await _depositRepository.CreateAsync(deposit);
            _logger.LogInformation("Solicitação de depósito criada: {DepositId} para vendedor {SellerId} - Valor: {Amount}",
                result.Id, result.SellerId, result.Amount);

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

            // Verificar se o depósito já foi processado
            if (deposit.Status != DepositStatus.Pending)
            {
                _logger.LogWarning("Tentativa de processar depósito {DepositId} com status {Status}",
                    deposit.Id, deposit.Status);
                return MapToDto(deposit);
            }

            // Verificar se o valor corresponde
            if (deposit.Amount != amount)
            {
                _logger.LogWarning("Valor do depósito {DepositId} não corresponde. Esperado: {Expected}, Recebido: {Received}",
                    deposit.Id, deposit.Amount, amount);

                // Aqui você pode decidir rejeitar ou aceitar com valor diferente
                // Por segurança, vamos considerar um erro
                throw new ValidationException($"Valor do pagamento não corresponde ao valor do depósito");
            }

            // Processar o status do pagamento
            if (status.ToLower() == "approved" || status.ToLower() == "completed")
            {
                // Atualizar o depósito
                deposit.Status = DepositStatus.Completed;
                deposit.ProcessedAt = DateTime.UtcNow;

                await _depositRepository.UpdateAsync(deposit);

                // Adicionar o valor à carteira do usuário
                await _walletService.AddFundsAsync(deposit.WalletId, new WalletOperationDto
                {
                    Amount = deposit.Amount,
                    Description = $"Depósito via PIX",
                    Reference = $"DEPOSIT_{deposit.Id}"
                });

                _logger.LogInformation("Depósito {DepositId} processado com sucesso. Valor: {Amount}",
                    deposit.Id, deposit.Amount);
            }
            else if (status.ToLower() == "failed" || status.ToLower() == "cancelled")
            {
                // Atualizar o depósito como falhou
                deposit.Status = DepositStatus.Failed;
                deposit.ProcessedAt = DateTime.UtcNow;

                await _depositRepository.UpdateAsync(deposit);

                _logger.LogInformation("Depósito {DepositId} falhou. Status: {Status}",
                    deposit.Id, status);
            }
            else
            {
                // Status desconhecido
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
