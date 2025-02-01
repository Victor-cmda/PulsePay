using Application.DTOs;
using Application.Interfaces;
using Domain.Interfaces;
using Domain.Models;
using Microsoft.Extensions.Logging;
using Shared.Enums;
using Shared.Exceptions;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Application.Services
{
    public class BankAccountService : IBankAccountService
    {
        private readonly IBankAccountRepository _bankAccountRepository;
        private readonly ILogger<BankAccountService> _logger;

        public BankAccountService(
            IBankAccountRepository bankAccountRepository,
            ILogger<BankAccountService> logger)
        {
            _bankAccountRepository = bankAccountRepository;
            _logger = logger;
        }

        public async Task<BankAccountResponseDto> GetBankAccountAsync(Guid id)
        {
            var bankAccount = await _bankAccountRepository.GetByIdAsync(id);
            if (bankAccount == null)
                throw new NotFoundException($"Bank account with ID {id} not found");

            return MapToResponseDto(bankAccount);
        }

        public async Task<IEnumerable<BankAccountResponseDto>> GetSellerBankAccountsAsync(Guid sellerId)
        {
            var bankAccounts = await _bankAccountRepository.GetBySellerIdAsync(sellerId);
            return bankAccounts.Select(MapToResponseDto);
        }

        public async Task<BankAccountResponseDto> CreateBankAccountAsync(BankAccountCreateDto createDto)
        {
            var validation = await ValidateBankAccountAsync(createDto);
            if (!validation.IsValid)
                throw new ValidationException(string.Join(", ", validation.ValidationErrors));

            // Verificar duplicidade baseada no tipo de conta
            bool exists = createDto.AccountType switch
            {
                BankAccountType.TED => await _bankAccountRepository.ExistsByAccountNumberAsync(
                    createDto.BankCode,
                    createDto.AccountNumber,
                    createDto.BranchNumber),
                BankAccountType.PIX => await _bankAccountRepository.ExistsByPixKeyAsync(
                    createDto.PixKey,
                    createDto.PixKeyType.Value),
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
                IsVerified = false
            };

            var created = await _bankAccountRepository.CreateAsync(bankAccount);
            _logger.LogInformation($"Bank account created: {created.Id} for seller {created.SellerId}");

            return MapToResponseDto(created);
        }

        public async Task<BankAccountResponseDto> UpdateBankAccountAsync(Guid id, BankAccountUpdateDto updateDto)
        {
            var bankAccount = await _bankAccountRepository.GetByIdAsync(id);
            if (bankAccount == null)
                throw new NotFoundException($"Bank account with ID {id} not found");

            // Atualizar campos comuns
            if (!string.IsNullOrEmpty(updateDto.BankName))
                bankAccount.BankName = updateDto.BankName;

            if (!string.IsNullOrEmpty(updateDto.BankCode))
                bankAccount.BankCode = updateDto.BankCode;

            if (!string.IsNullOrEmpty(updateDto.AccountHolderName))
                bankAccount.AccountHolderName = updateDto.AccountHolderName;

            // Atualizar campos específicos baseado no tipo de conta
            switch (bankAccount.AccountType)
            {
                case BankAccountType.TED:
                    if (!string.IsNullOrEmpty(updateDto.AccountNumber))
                    {
                        if (!IsValidAccountNumber(updateDto.AccountNumber))
                            throw new ValidationException("Invalid account number format");
                        bankAccount.AccountNumber = updateDto.AccountNumber;
                    }

                    if (!string.IsNullOrEmpty(updateDto.BranchNumber))
                    {
                        if (!IsValidBranchNumber(updateDto.BranchNumber))
                            throw new ValidationException("Invalid branch number format");
                        bankAccount.BranchNumber = updateDto.BranchNumber;
                    }
                    break;

                case BankAccountType.PIX:
                    if (!string.IsNullOrEmpty(updateDto.PixKey))
                    {
                        if (!IsValidPixKey(updateDto.PixKey, updateDto.PixKeyType ?? bankAccount.PixKeyType))
                            throw new ValidationException("Invalid PIX key format");
                        bankAccount.PixKey = updateDto.PixKey;
                        bankAccount.PixKeyType = updateDto.PixKeyType ?? bankAccount.PixKeyType;
                    }
                    break;
            }

            var updated = await _bankAccountRepository.UpdateAsync(bankAccount);
            _logger.LogInformation($"Bank account updated: {updated.Id}");

            return MapToResponseDto(updated);
        }

        public async Task<bool> DeleteBankAccountAsync(Guid id, Guid sellerId)
        {
            var isOwner = await _bankAccountRepository.IsOwnerAsync(id, sellerId);
            if (!isOwner)
                throw new UnauthorizedAccessException("You are not authorized to delete this bank account");

            var result = await _bankAccountRepository.DeleteAsync(id);
            if (result)
                _logger.LogInformation($"Bank account deleted: {id}");

            return result;
        }

        public async Task<BankAccountValidationDto> ValidateBankAccountAsync(BankAccountCreateDto createDto)
        {
            var validation = new BankAccountValidationDto { IsValid = true };

            // Validações comuns
            if (!IsValidDocument(createDto.DocumentNumber))
                validation.ValidationErrors.Add("Invalid document number");

            if (string.IsNullOrEmpty(createDto.BankCode) || !IsValidBankCode(createDto.BankCode))
                validation.ValidationErrors.Add("Invalid bank code");

            if (string.IsNullOrEmpty(createDto.BankName))
                validation.ValidationErrors.Add("Bank name is required");

            if (string.IsNullOrEmpty(createDto.AccountHolderName))
                validation.ValidationErrors.Add("Account holder name is required");

            // Validações específicas por tipo de conta
            switch (createDto.AccountType)
            {
                case BankAccountType.TED:
                    if (string.IsNullOrEmpty(createDto.AccountNumber) ||
                        string.IsNullOrEmpty(createDto.BranchNumber))
                    {
                        validation.ValidationErrors.Add("Account number and branch are required for TED");
                    }

                    if (!IsValidAccountNumber(createDto.AccountNumber))
                        validation.ValidationErrors.Add("Invalid account number");

                    if (!IsValidBranchNumber(createDto.BranchNumber))
                        validation.ValidationErrors.Add("Invalid branch number");
                    break;

                case BankAccountType.PIX:
                    if (string.IsNullOrEmpty(createDto.PixKey))
                    {
                        validation.ValidationErrors.Add("PIX key is required for PIX type accounts");
                    }

                    if (!createDto.PixKeyType.HasValue)
                    {
                        validation.ValidationErrors.Add("PIX key type is required");
                    }

                    if (!IsValidPixKey(createDto.PixKey, createDto.PixKeyType))
                    {
                        validation.ValidationErrors.Add("Invalid PIX key format");
                    }
                    break;
            }

            validation.IsValid = !validation.ValidationErrors.Any();
            return validation;
        }

        public async Task<bool> VerifyBankAccountAsync(Guid id)
        {
            var bankAccount = await _bankAccountRepository.GetByIdAsync(id);
            if (bankAccount == null)
                throw new NotFoundException($"Bank account with ID {id} not found");

            bankAccount.IsVerified = true;
            await _bankAccountRepository.UpdateAsync(bankAccount);

            _logger.LogInformation($"Bank account verified: {id}");
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

        private bool IsValidDocument(string document)
        {
            document = new string(document.Where(char.IsDigit).ToArray());

            return document.Length == 11 ? IsCpf(document) : IsCnpj(document);
        }

        private bool IsCpf(string cpf)
        {
            if (string.IsNullOrEmpty(cpf) || cpf.Length != 11)
                return false;

            // Validação do CPF
            int[] multiplicador1 = new int[9] { 10, 9, 8, 7, 6, 5, 4, 3, 2 };
            int[] multiplicador2 = new int[10] { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 };

            cpf = cpf.Trim().Replace(".", "").Replace("-", "");
            if (cpf.Length != 11)
                return false;

            string tempCpf = cpf.Substring(0, 9);
            int soma = 0;

            for (int i = 0; i < 9; i++)
                soma += int.Parse(tempCpf[i].ToString()) * multiplicador1[i];

            int resto = soma % 11;
            if (resto < 2)
                resto = 0;
            else
                resto = 11 - resto;

            string digito = resto.ToString();
            tempCpf = tempCpf + digito;
            soma = 0;
            for (int i = 0; i < 10; i++)
                soma += int.Parse(tempCpf[i].ToString()) * multiplicador2[i];

            resto = soma % 11;
            if (resto < 2)
                resto = 0;
            else
                resto = 11 - resto;

            digito = digito + resto.ToString();

            return cpf.EndsWith(digito);
        }

        private bool IsCnpj(string cnpj)
        {
            if (string.IsNullOrEmpty(cnpj) || cnpj.Length != 14)
                return false;

            // Validação do CNPJ
            int[] multiplicador1 = new int[12] { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
            int[] multiplicador2 = new int[13] { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

            cnpj = cnpj.Trim().Replace(".", "").Replace("-", "").Replace("/", "");
            if (cnpj.Length != 14)
                return false;

            string tempCnpj = cnpj.Substring(0, 12);
            int soma = 0;

            for (int i = 0; i < 12; i++)
                soma += int.Parse(tempCnpj[i].ToString()) * multiplicador1[i];

            int resto = (soma % 11);
            if (resto < 2)
                resto = 0;
            else
                resto = 11 - resto;

            string digito = resto.ToString();
            tempCnpj = tempCnpj + digito;
            soma = 0;
            for (int i = 0; i < 13; i++)
                soma += int.Parse(tempCnpj[i].ToString()) * multiplicador2[i];

            resto = (soma % 11);
            if (resto < 2)
                resto = 0;
            else
                resto = 11 - resto;

            digito = digito + resto.ToString();

            return cnpj.EndsWith(digito);
        }

        private bool IsValidBankCode(string bankCode)
        {
            return !string.IsNullOrEmpty(bankCode) &&
                   bankCode.Length == 3 &&
                   bankCode.All(char.IsDigit);
        }

        private bool IsValidAccountNumber(string accountNumber)
        {
            return !string.IsNullOrEmpty(accountNumber) &&
                   accountNumber.Length <= 20 &&
                   Regex.IsMatch(accountNumber, @"^\d+(-[\dxX])?$");
        }

        private bool IsValidBranchNumber(string branchNumber)
        {
            return !string.IsNullOrEmpty(branchNumber) &&
                   branchNumber.Length <= 10 &&
                   Regex.IsMatch(branchNumber, @"^\d+(-[\dxX])?$");
        }

        private bool IsValidPixKey(string pixKey, PixKeyType? pixKeyType)
        {
            if (string.IsNullOrEmpty(pixKey) || !pixKeyType.HasValue)
                return false;

            return pixKeyType switch
            {
                PixKeyType.CPF => IsCpf(pixKey),
                PixKeyType.CNPJ => IsCnpj(pixKey),
                PixKeyType.EMAIL => IsValidEmail(pixKey),
                PixKeyType.PHONE => IsValidPhone(pixKey),
                PixKeyType.RANDOM => IsValidRandomKey(pixKey),
                _ => false
            };
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private bool IsValidPhone(string phone)
        {
            // Formato: +55DDD999999999
            return Regex.IsMatch(phone, @"^\+55\d{11}$");
        }

        private bool IsValidRandomKey(string key)
        {
            // Chave aleatória do PIX tem 32 caracteres
            return key.Length == 32 &&
                   Regex.IsMatch(key, @"^[a-zA-Z0-9]+$");
        }
    }
}
