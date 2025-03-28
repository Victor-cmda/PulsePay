using Application.DTOs;
using Application.Interfaces;
using Domain.Interfaces;
using Domain.Models;
using Microsoft.Extensions.Logging;
using Shared.Enums;
using Shared.Exceptions;
using ValidationException = Shared.Exceptions.ValidationException;

namespace Application.Services
{
    public class WalletService : IWalletService
    {
        private readonly IWalletRepository _walletRepository;
        private readonly IWalletTransactionRepository _transactionRepository;
        private readonly ILogger<WalletService> _logger;

        public WalletService(
            IWalletRepository walletRepository,
            IWalletTransactionRepository transactionRepository,
            ILogger<WalletService> logger)
        {
            _walletRepository = walletRepository ?? throw new ArgumentNullException(nameof(walletRepository));
            _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<WalletDto> GetWalletAsync(Guid id)
        {
            var wallet = await _walletRepository.GetByIdAsync(id);
            if (wallet == null)
                throw new NotFoundException($"Carteira com ID {id} não encontrada");

            return MapToDto(wallet);
        }

        public async Task<WalletDto> GetWalletByTypeAsync(Guid sellerId, WalletType walletType)
        {
            var wallet = await _walletRepository.GetBySellerIdAndTypeAsync(sellerId, walletType);
            if (wallet == null)
                throw new NotFoundException($"Carteira do tipo {walletType} não encontrada para o vendedor {sellerId}");

            return MapToDto(wallet);
        }

        public async Task<IEnumerable<WalletDto>> GetSellerWalletsAsync(Guid sellerId)
        {
            var wallets = await _walletRepository.GetAllBySellerIdAsync(sellerId);
            return wallets.Select(MapToDto);
        }

        public async Task<WalletWithTransactionsDto> GetWalletWithRecentTransactionsAsync(Guid walletId, int count = 10)
        {
            var wallet = await _walletRepository.GetByIdAsync(walletId);
            if (wallet == null)
                throw new NotFoundException($"Carteira com ID {walletId} não encontrada");

            var transactions = await _transactionRepository.GetRecentByWalletIdAsync(wallet.Id, count);

            return new WalletWithTransactionsDto
            {
                Wallet = MapToDto(wallet),
                RecentTransactions = transactions.Select(MapTransactionToDto).ToList()
            };
        }

        // Em WalletService.cs, no método CreateWalletAsync

        public async Task<WalletDto> CreateWalletAsync(WalletCreateDto createDto)
        {
            // Verificar se já existe uma carteira do mesmo tipo para este vendedor
            var exists = await _walletRepository.ExistsAsync(createDto.SellerId, createDto.WalletType);
            if (exists)
                throw new ConflictException($"Já existe uma carteira do tipo {createDto.WalletType} para o vendedor {createDto.SellerId}");

            // Verificar regras especiais para carteiras General
            if (createDto.WalletType == WalletType.General)
            {
                // Se o vendedor tenta criar uma carteira General, ele não pode ter outras carteiras
                var walletCount = await _walletRepository.CountBySellerIdAsync(createDto.SellerId);
                if (walletCount > 0)
                    throw new ConflictException("Não é possível criar uma carteira do tipo General se já existem outras carteiras");
            }
            else
            {
                // Se o vendedor tenta criar carteiras especializadas, ele não pode ter uma carteira General
                var hasGeneralWallet = await _walletRepository.ExistsAsync(createDto.SellerId, WalletType.General);
                if (hasGeneralWallet)
                    throw new ConflictException("Não é possível criar carteiras especializadas quando já existe uma carteira do tipo General");

                // Verificar se já atingiu o limite de 2 carteiras especializadas
                var walletCount = await _walletRepository.CountBySellerIdAsync(createDto.SellerId);
                if (walletCount >= 2)
                    throw new ConflictException($"O vendedor {createDto.SellerId} já possui o número máximo de carteiras especializadas (2)");
            }

            // Se for a primeira carteira, torná-la padrão
            var anyWallets = await _walletRepository.CountBySellerIdAsync(createDto.SellerId) > 0;
            bool isDefault = !anyWallets || createDto.IsDefault;

            // Se esta carteira for definida como padrão e já existir outra carteira,
            // teremos que atualizar a outra carteira para não ser mais a padrão
            if (isDefault && anyWallets)
            {
                var existingWallets = await _walletRepository.GetAllBySellerIdAsync(createDto.SellerId);
                foreach (var existingWallet in existingWallets)
                {
                    if (existingWallet.IsDefault)
                    {
                        existingWallet.IsDefault = false;
                        await _walletRepository.UpdateAsync(existingWallet);
                    }
                }
            }

            var wallet = new Wallet
            {
                Id = Guid.NewGuid(),
                SellerId = createDto.SellerId,
                WalletType = createDto.WalletType,
                IsDefault = isDefault,
                AvailableBalance = 0,
                PendingBalance = 0,
                TotalBalance = 0,
                CreatedAt = DateTime.UtcNow,
                LastUpdateAt = DateTime.UtcNow
            };

            var created = await _walletRepository.CreateAsync(wallet);
            _logger.LogInformation("Carteira do tipo {WalletType} criada para o vendedor {SellerId}",
                createDto.WalletType, createDto.SellerId);

            return MapToDto(created);
        }

        public async Task<WalletDto> UpdateBalanceAsync(Guid walletId, WalletUpdateDto updateDto)
        {
            var wallet = await _walletRepository.GetByIdAsync(walletId);
            if (wallet == null)
                throw new NotFoundException($"Carteira com ID {walletId} não encontrada");

            wallet.AvailableBalance = updateDto.AvailableBalance;
            wallet.PendingBalance = updateDto.PendingBalance;
            wallet.TotalBalance = wallet.AvailableBalance + wallet.PendingBalance;
            wallet.LastUpdateAt = DateTime.UtcNow;

            var updated = await _walletRepository.UpdateAsync(wallet);
            _logger.LogInformation("Saldo atualizado para a carteira {WalletId} do vendedor {SellerId}",
                walletId, wallet.SellerId);

            return MapToDto(updated);
        }

        public async Task<WalletDto> SetDefaultWalletAsync(Guid walletId, Guid sellerId)
        {
            var wallet = await _walletRepository.GetByIdAsync(walletId);
            if (wallet == null || wallet.SellerId != sellerId)
                throw new NotFoundException($"Carteira {walletId} não encontrada para o vendedor {sellerId}");

            // Já é a padrão
            if (wallet.IsDefault)
                return MapToDto(wallet);

            // Começar uma transação para garantir consistência
            await using var transaction = await _walletRepository.BeginTransactionAsync();

            try
            {
                // Buscar todas as carteiras do vendedor e remover a flag padrão
                var wallets = await _walletRepository.GetAllBySellerIdAsync(sellerId);
                foreach (var w in wallets)
                {
                    if (w.IsDefault && w.Id != walletId)
                    {
                        w.IsDefault = false;
                        await _walletRepository.UpdateAsync(w);
                    }
                }

                // Definir a carteira atual como padrão
                wallet.IsDefault = true;
                wallet.LastUpdateAt = DateTime.UtcNow;
                var updated = await _walletRepository.UpdateAsync(wallet);

                await transaction.CommitAsync();

                _logger.LogInformation("Carteira {WalletId} definida como padrão para o vendedor {SellerId}",
                    walletId, sellerId);

                return MapToDto(updated);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Erro ao definir a carteira {WalletId} como padrão", walletId);
                throw;
            }
        }

        public async Task<WalletDto> AddFundsAsync(Guid walletId, WalletOperationDto operationDto)
        {
            if (operationDto.Amount <= 0)
                throw new ValidationException("O valor deve ser maior que zero");

            var wallet = await _walletRepository.GetByIdAsync(walletId);
            if (wallet == null)
                throw new NotFoundException($"Carteira com ID {walletId} não encontrada");

            // Verify wallet type restriction - only Deposit or General wallets can receive funds
            if (wallet.WalletType == WalletType.Withdrawal)
                throw new ValidationException("Não é possível adicionar fundos a uma carteira do tipo Saque (Withdrawal). Use a carteira de Depósito (Deposit) ou Geral (General).");

            // Create the transaction
            var transaction = new WalletTransaction
            {
                Id = Guid.NewGuid(),
                WalletId = wallet.Id,
                Amount = operationDto.Amount,
                Type = TransactionType.Deposit,
                Status = TransactionStatus.Completed,
                Description = operationDto.Description ?? "Depósito de fundos",
                Reference = operationDto.Reference,
                CreatedAt = DateTime.UtcNow,
                ProcessedAt = DateTime.UtcNow
            };

            // Update wallet balance
            wallet.AvailableBalance += operationDto.Amount;
            wallet.TotalBalance = wallet.AvailableBalance + wallet.PendingBalance;
            wallet.LastUpdateAt = DateTime.UtcNow;

            // Save changes in transaction
            await using var dbTransaction = await _walletRepository.BeginTransactionAsync();
            try
            {
                await _transactionRepository.CreateAsync(transaction);
                var updated = await _walletRepository.UpdateAsync(wallet);

                await dbTransaction.CommitAsync();

                _logger.LogInformation("Adicionado {Amount} à carteira {WalletId} do tipo {WalletType}",
                                      operationDto.Amount, wallet.Id, wallet.WalletType);
                return MapToDto(updated);
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();
                _logger.LogError(ex, "Erro ao adicionar fundos à carteira {WalletId}", wallet.Id);
                throw;
            }
        }

        public async Task<WalletDto> DeductFundsAsync(Guid walletId, WalletOperationDto operationDto)
        {
            if (operationDto.Amount <= 0)
                throw new ValidationException("O valor deve ser maior que zero");

            var wallet = await _walletRepository.GetByIdAsync(walletId);
            if (wallet == null)
                throw new NotFoundException($"Carteira com ID {walletId} não encontrada");

            // Verify wallet type restriction - only Withdrawal or General wallets can send funds
            if (wallet.WalletType == WalletType.Deposit)
                throw new ValidationException("Não é possível deduzir fundos de uma carteira do tipo Depósito (Deposit). Use a carteira de Saque (Withdrawal) ou Geral (General).");

            if (wallet.AvailableBalance < operationDto.Amount)
                throw new InsufficientFundsException("Saldo insuficiente para esta operação");

            // Create the transaction
            var transaction = new WalletTransaction
            {
                Id = Guid.NewGuid(),
                WalletId = wallet.Id,
                Amount = operationDto.Amount,
                Type = TransactionType.Withdrawal,
                Status = TransactionStatus.Completed,
                Description = operationDto.Description ?? "Retirada de fundos",
                Reference = operationDto.Reference,
                CreatedAt = DateTime.UtcNow,
                ProcessedAt = DateTime.UtcNow
            };

            // Update wallet balance
            wallet.AvailableBalance -= operationDto.Amount;
            wallet.TotalBalance = wallet.AvailableBalance + wallet.PendingBalance;
            wallet.LastUpdateAt = DateTime.UtcNow;

            // Save changes in transaction
            await using var dbTransaction = await _walletRepository.BeginTransactionAsync();
            try
            {
                await _transactionRepository.CreateAsync(transaction);
                var updated = await _walletRepository.UpdateAsync(wallet);

                await dbTransaction.CommitAsync();

                _logger.LogInformation("Deduzido {Amount} da carteira {WalletId} do tipo {WalletType}",
                                      operationDto.Amount, wallet.Id, wallet.WalletType);
                return MapToDto(updated);
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();
                _logger.LogError(ex, "Erro ao deduzir fundos da carteira {WalletId}", wallet.Id);
                throw;
            }
        }

        public async Task<List<WalletTransactionDto>> GetTransactionsAsync(Guid walletId, DateTime? startDate = null, DateTime? endDate = null, int page = 1, int pageSize = 20)
        {
            var wallet = await _walletRepository.GetByIdAsync(walletId);
            if (wallet == null)
                throw new NotFoundException($"Carteira com ID {walletId} não encontrada");

            var transactions = await _transactionRepository.GetByWalletIdAsync(
                wallet.Id,
                startDate,
                endDate,
                page,
                pageSize);

            return transactions.Select(MapTransactionToDto).ToList();
        }

        public async Task<bool> HasSufficientFundsAsync(Guid walletId, decimal amount)
        {
            var wallet = await _walletRepository.GetByIdAsync(walletId);
            return wallet?.AvailableBalance >= amount;
        }

        public async Task<decimal> GetAvailableBalanceAsync(Guid walletId)
        {
            var wallet = await _walletRepository.GetByIdAsync(walletId);
            if (wallet == null)
                throw new NotFoundException($"Carteira com ID {walletId} não encontrada");

            return wallet.AvailableBalance;
        }

        // Método auxiliar para transferir saldo entre carteiras (mesmo vendedor)
        public async Task<(WalletDto sourceWallet, WalletDto destinationWallet)> TransferBetweenWalletsAsync(
    Guid sourceWalletId,
    Guid destinationWalletId,
    decimal amount,
    string description = null)
        {
            if (amount <= 0)
                throw new ValidationException("O valor da transferência deve ser maior que zero");

            if (sourceWalletId == destinationWalletId)
                throw new ValidationException("As carteiras de origem e destino devem ser diferentes");

            var sourceWallet = await _walletRepository.GetByIdAsync(sourceWalletId);
            if (sourceWallet == null)
                throw new NotFoundException($"Carteira de origem com ID {sourceWalletId} não encontrada");

            var destinationWallet = await _walletRepository.GetByIdAsync(destinationWalletId);
            if (destinationWallet == null)
                throw new NotFoundException($"Carteira de destino com ID {destinationWalletId} não encontrada");

            // Check if the wallets belong to the same seller
            if (sourceWallet.SellerId != destinationWallet.SellerId)
                throw new ValidationException("Transferências só são permitidas entre carteiras do mesmo vendedor");

            // Verify if transfer direction is valid based on wallet types
            bool isValidTransfer = false;

            // Specific transfer validation rules
            if (sourceWallet.WalletType == WalletType.General && destinationWallet.WalletType == WalletType.General)
            {
                // Transfer between General wallets (unlikely, but possible)
                isValidTransfer = true;
            }
            else if (sourceWallet.WalletType == WalletType.General)
            {
                // From General to any other
                isValidTransfer = true;
            }
            else if (destinationWallet.WalletType == WalletType.General)
            {
                // From any other to General
                isValidTransfer = true;
            }
            else if (sourceWallet.WalletType == WalletType.Deposit && destinationWallet.WalletType == WalletType.Withdrawal)
            {
                // From Deposit to Withdrawal (main flow)
                isValidTransfer = true;
            }
            // New rule: Never allow transfers from Withdrawal to Deposit
            else if (sourceWallet.WalletType == WalletType.Withdrawal && destinationWallet.WalletType == WalletType.Deposit)
            {
                isValidTransfer = false;
            }

            if (!isValidTransfer)
            {
                throw new ValidationException($"Transferência de {sourceWallet.WalletType} para {destinationWallet.WalletType} não permitida. " +
                                               "Fluxos permitidos: Depósito → Saque, Geral → qualquer, qualquer → Geral");
            }

            // Check balance
            if (sourceWallet.AvailableBalance < amount)
                throw new InsufficientFundsException("Saldo insuficiente na carteira de origem");

            // Start transaction
            await using var transaction = await _walletRepository.BeginTransactionAsync();

            try
            {
                // Create outgoing transaction
                var outTransaction = new WalletTransaction
                {
                    Id = Guid.NewGuid(),
                    WalletId = sourceWallet.Id,
                    Amount = amount,
                    Type = TransactionType.Withdraw,
                    Status = TransactionStatus.Completed,
                    Description = description ?? $"Transferência para carteira {destinationWallet.WalletType}",
                    Reference = $"TRANSFER_TO_{destinationWallet.Id}",
                    CreatedAt = DateTime.UtcNow,
                    ProcessedAt = DateTime.UtcNow
                };

                // Create incoming transaction
                var inTransaction = new WalletTransaction
                {
                    Id = Guid.NewGuid(),
                    WalletId = destinationWallet.Id,
                    Amount = amount,
                    Type = TransactionType.Deposit,
                    Status = TransactionStatus.Completed,
                    Description = description ?? $"Transferência da carteira {sourceWallet.WalletType}",
                    Reference = $"TRANSFER_FROM_{sourceWallet.Id}",
                    CreatedAt = DateTime.UtcNow,
                    ProcessedAt = DateTime.UtcNow
                };

                // Update balances
                sourceWallet.AvailableBalance -= amount;
                sourceWallet.TotalBalance = sourceWallet.AvailableBalance + sourceWallet.PendingBalance;
                sourceWallet.LastUpdateAt = DateTime.UtcNow;

                destinationWallet.AvailableBalance += amount;
                destinationWallet.TotalBalance = destinationWallet.AvailableBalance + destinationWallet.PendingBalance;
                destinationWallet.LastUpdateAt = DateTime.UtcNow;

                // Save transactions
                await _transactionRepository.CreateAsync(outTransaction);
                await _transactionRepository.CreateAsync(inTransaction);

                // Update wallets
                var updatedSourceWallet = await _walletRepository.UpdateAsync(sourceWallet);
                var updatedDestinationWallet = await _walletRepository.UpdateAsync(destinationWallet);

                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Transferência de {Amount} da carteira {SourceWalletType} para a carteira {DestinationWalletType}",
                    amount, sourceWallet.WalletType, destinationWallet.WalletType);

                return (MapToDto(updatedSourceWallet), MapToDto(updatedDestinationWallet));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex,
                    "Erro ao transferir {Amount} da carteira {SourceWalletId} para a carteira {DestinationWalletId}",
                    amount, sourceWalletId, destinationWalletId);
                throw;
            }
        }

        public async Task<IEnumerable<WalletDto>> GetAllWalletsAsync(int page = 1, int pageSize = 20)
        {
            var wallets = await _walletRepository.GetAllAsync(page, pageSize);
            return wallets.Select(MapToDto);
        }

        public async Task<int> GetTotalWalletCountAsync()
        {
            return await _walletRepository.GetTotalCountAsync();
        }

        public async Task<decimal> GetTotalSystemBalanceAsync()
        {
            return await _walletRepository.GetTotalSystemBalanceAsync();
        }

        private static WalletDto MapToDto(Wallet wallet)
        {
            return new WalletDto
            {
                Id = wallet.Id,
                SellerId = wallet.SellerId,
                AvailableBalance = wallet.AvailableBalance,
                PendingBalance = wallet.PendingBalance,
                TotalBalance = wallet.TotalBalance,
                WalletType = wallet.WalletType,
                IsDefault = wallet.IsDefault,
                LastUpdateAt = wallet.LastUpdateAt,
                CreatedAt = wallet.CreatedAt
            };
        }

        private static WalletTransactionDto MapTransactionToDto(WalletTransaction transaction)
        {
            return new WalletTransactionDto
            {
                Id = transaction.Id,
                WalletId = transaction.WalletId,
                Amount = transaction.Amount,
                Type = transaction.Type.ToString(),
                Status = transaction.Status.ToString(),
                Description = transaction.Description,
                Reference = transaction.Reference,
                CreatedAt = transaction.CreatedAt,
                ProcessedAt = transaction.ProcessedAt
            };
        }
    }
}