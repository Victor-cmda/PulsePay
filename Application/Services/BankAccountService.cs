using Application.DTOs;
using Application.Interfaces;
using Domain.Interfaces;
using Domain.Models;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Shared.Enums;
using Shared.Exceptions;
using ValidationException = FluentValidation.ValidationException;

namespace Application.Services
{
    public class BankAccountService : IBankAccountService
    {
        private readonly IBankAccountRepository _bankAccountRepository;
        private readonly ILogger<BankAccountService> _logger;
        private readonly IValidator<BankAccountCreateDto> _createValidator;
        private readonly IValidator<BankAccountUpdateDto> _updateValidator;

        public BankAccountService(
            IBankAccountRepository bankAccountRepository,
            ILogger<BankAccountService> logger,
            IValidator<BankAccountCreateDto> createValidator,
            IValidator<BankAccountUpdateDto> updateValidator)
        {
            _bankAccountRepository = bankAccountRepository;
            _logger = logger;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
        }

        public async Task<BankAccountResponseDto> GetBankAccountAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var bankAccount = await _bankAccountRepository.GetByIdAsync(id, cancellationToken);
            if (bankAccount == null)
                throw new NotFoundException($"Bank account with ID {id} not found");

            return MapToResponseDto(bankAccount);
        }

        public async Task<IReadOnlyCollection<BankAccountResponseDto>> GetSellerBankAccountsAsync(Guid sellerId, CancellationToken cancellationToken = default)
        {
            var bankAccounts = await _bankAccountRepository.GetBySellerIdAsync(sellerId, cancellationToken);
            return bankAccounts.Select(MapToResponseDto).ToList().AsReadOnly();
        }

        public async Task<BankAccountResponseDto> CreateBankAccountAsync(BankAccountCreateDto createDto, CancellationToken cancellationToken = default)
        {
            createDto = NormalizeCreateDto(createDto);

            var validationResult = await _createValidator.ValidateAsync(createDto, cancellationToken);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
            }

            bool exists = createDto.AccountType switch
            {
                BankAccountType.TED => await _bankAccountRepository.ExistsByAccountNumberAsync(
                    createDto.BankCode,
                    createDto.AccountNumber,
                    createDto.BranchNumber,
                    cancellationToken),
                BankAccountType.PIX => await _bankAccountRepository.ExistsByPixKeyAsync(
                    createDto.PixKey,
                    createDto.PixKeyType.Value,
                    cancellationToken),
                _ => false
            };

            if (exists)
                throw new ConflictException("Bank account already registered");

            var bankAccount = new BankAccount
            {
                Id = Guid.NewGuid(),
                SellerId = createDto.SellerId,
                BankName = createDto.BankName,
                BankCode = createDto.BankCode,
                AccountType = createDto.AccountType,
                AccountNumber = createDto.AccountNumber,
                BranchNumber = createDto.BranchNumber,
                PixKey = createDto.PixKey,
                PixKeyType = createDto.PixKeyType,
                DocumentNumber = createDto.DocumentNumber,
                AccountHolderName = createDto.AccountHolderName,
                IsVerified = false,
                CreatedAt = DateTime.UtcNow,
                LastUpdatedAt = DateTime.UtcNow
            };

            var created = await _bankAccountRepository.CreateAsync(bankAccount, cancellationToken);
            _logger.LogInformation($"Bank account created: {created.Id} for seller {created.SellerId}");

            return MapToResponseDto(created);
        }

        public async Task<BankAccountResponseDto> UpdateBankAccountAsync(Guid id, BankAccountUpdateDto updateDto, CancellationToken cancellationToken = default)
        {
            var validationResult = await _updateValidator.ValidateAsync(updateDto, cancellationToken);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
            }

            var bankAccount = await _bankAccountRepository.GetByIdAsync(id, cancellationToken);
            if (bankAccount == null)
                throw new NotFoundException($"Bank account with ID {id} not found");

            if (!string.IsNullOrEmpty(updateDto.BankName))
                bankAccount.BankName = updateDto.BankName;

            if (!string.IsNullOrEmpty(updateDto.BankCode))
                bankAccount.BankCode = updateDto.BankCode;

            if (!string.IsNullOrEmpty(updateDto.AccountHolderName))
                bankAccount.AccountHolderName = updateDto.AccountHolderName;

            if (!string.IsNullOrEmpty(updateDto.DocumentNumber))
                bankAccount.DocumentNumber = updateDto.DocumentNumber;

            // Atualizar campos específicos baseado no tipo de conta
            switch (bankAccount.AccountType)
            {
                case BankAccountType.TED:
                    if (!string.IsNullOrEmpty(updateDto.AccountNumber))
                        bankAccount.AccountNumber = updateDto.AccountNumber;

                    if (!string.IsNullOrEmpty(updateDto.BranchNumber))
                        bankAccount.BranchNumber = updateDto.BranchNumber;
                    break;

                case BankAccountType.PIX:
                    if (!string.IsNullOrEmpty(updateDto.PixKey))
                    {
                        // Verificar se já existe outro registro com a mesma chave PIX
                        if (await _bankAccountRepository.ExistsByPixKeyAsync(
                            updateDto.PixKey,
                            updateDto.PixKeyType ?? bankAccount.PixKeyType.Value,
                            cancellationToken))
                        {
                            throw new ConflictException("PIX key already registered with another account");
                        }

                        bankAccount.PixKey = updateDto.PixKey;
                        bankAccount.PixKeyType = updateDto.PixKeyType ?? bankAccount.PixKeyType;
                    }
                    break;
            }

            var updated = await _bankAccountRepository.UpdateAsync(bankAccount, cancellationToken);
            _logger.LogInformation("Bank account updated: {Id}", updated.Id);

            return MapToResponseDto(updated);
        }

        public async Task<bool> DeleteBankAccountAsync(Guid id, Guid sellerId, CancellationToken cancellationToken = default)
        {
            var isOwner = await _bankAccountRepository.IsOwnerAsync(id, sellerId, cancellationToken);
            if (!isOwner)
                throw new UnauthorizedException("You are not authorized to delete this bank account");

            var result = await _bankAccountRepository.DeleteAsync(id, cancellationToken);
            if (result)
                _logger.LogInformation("Bank account deleted: {Id}", id);

            return result;
        }

        public async Task<BankAccountValidationDto> ValidateBankAccountAsync(BankAccountCreateDto createDto, CancellationToken cancellationToken = default)
        {
            var validation = new BankAccountValidationDto { IsValid = true };

            var validationResult = await _createValidator.ValidateAsync(createDto, cancellationToken);

            if (!validationResult.IsValid)
            {
                validation.IsValid = false;
                validation.ValidationErrors = validationResult.Errors
                    .Select(e => e.ErrorMessage)
                    .ToList();
            }

            return validation;
        }

        public async Task<bool> VerifyBankAccountAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var bankAccount = await _bankAccountRepository.GetByIdAsync(id, cancellationToken);
            if (bankAccount == null)
                throw new NotFoundException($"Bank account with ID {id} not found");

            bankAccount.IsVerified = true;
            await _bankAccountRepository.UpdateAsync(bankAccount, cancellationToken);

            _logger.LogInformation("Bank account verified: {Id}", id);
            return true;
        }

        public async Task<IEnumerable<BankAccountResponseDto>> GetUnverifiedAccountsAsync(int page = 1, int pageSize = 20)
        {
            var accounts = await _bankAccountRepository.GetUnverifiedAccountsAsync(page, pageSize);
            return accounts.Select(MapToResponseDto);
        }

        public async Task<int> GetTotalAccountsCountAsync()
        {
            return await _bankAccountRepository.GetTotalCountAsync();
        }

        public async Task<bool> RejectBankAccountAsync(Guid id, string reason, CancellationToken cancellationToken = default)
        {
            var bankAccount = await _bankAccountRepository.GetByIdAsync(id, cancellationToken);
            if (bankAccount == null)
                throw new NotFoundException($"Conta bancária com ID {id} não encontrada");

            bankAccount.Status = "Rejected";
            bankAccount.RejectionReason = reason;
            bankAccount.LastUpdatedAt = DateTime.UtcNow;

            await _bankAccountRepository.UpdateAsync(bankAccount, cancellationToken);

            _logger.LogInformation($"Conta bancária {id} rejeitada. Motivo: {reason}");

            return true;
        }

        private BankAccountResponseDto MapToResponseDto(BankAccount bankAccount)
        {
            return new BankAccountResponseDto
            {
                Id = bankAccount.Id,
                SellerId = bankAccount.SellerId,
                BankName = bankAccount.BankName,
                BankCode = bankAccount.BankCode,
                AccountType = bankAccount.AccountType,
                AccountNumber = bankAccount.AccountNumber,
                BranchNumber = bankAccount.BranchNumber,
                PixKey = bankAccount.PixKey,
                PixKeyType = bankAccount.PixKeyType,
                DocumentNumber = bankAccount.DocumentNumber,
                AccountHolderName = bankAccount.AccountHolderName,
                IsVerified = bankAccount.IsVerified,
                CreatedAt = bankAccount.CreatedAt,
                LastUpdatedAt = bankAccount.LastUpdatedAt
            };
        }

        private BankAccountCreateDto NormalizeCreateDto(BankAccountCreateDto dto)
        {
            var normalized = new BankAccountCreateDto
            {
                SellerId = dto.SellerId,
                BankName = dto.BankName,
                BankCode = dto.BankCode,
                AccountType = dto.AccountType,
                AccountHolderName = dto.AccountHolderName,
                DocumentNumber = new string(dto.DocumentNumber.Where(char.IsDigit).ToArray())
            };

            if (dto.AccountType == BankAccountType.TED)
            {
                normalized.AccountNumber = new string(dto.AccountNumber.Where(c => char.IsDigit(c) || c == '-' || c == 'X' || c == 'x').ToArray());
                normalized.BranchNumber = new string(dto.BranchNumber.Where(c => char.IsDigit(c) || c == '-' || c == 'X' || c == 'x').ToArray());
            }
            else if (dto.AccountType == BankAccountType.PIX)
            {
                normalized.PixKeyType = dto.PixKeyType;

                if (dto.PixKeyType == PixKeyType.CPF || dto.PixKeyType == PixKeyType.CNPJ)
                {
                    normalized.PixKey = new string(dto.PixKey.Where(char.IsDigit).ToArray());
                }
                else
                {
                    normalized.PixKey = dto.PixKey;
                }
            }

            return normalized;
        }

    }
}