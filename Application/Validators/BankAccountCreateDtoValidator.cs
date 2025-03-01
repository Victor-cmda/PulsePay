using Application.DTOs;
using FluentValidation;
using Shared.Enums;
using System.Text.RegularExpressions;

namespace Application.Validators
{
    public class BankAccountCreateDtoValidator : AbstractValidator<BankAccountCreateDto>
    {
        private readonly IDocumentValidator _documentValidator;
        private readonly IPixKeyValidator _pixKeyValidator;

        public BankAccountCreateDtoValidator(IDocumentValidator documentValidator, IPixKeyValidator pixKeyValidator)
        {
            _documentValidator = documentValidator;
            _pixKeyValidator = pixKeyValidator;

            // Validações comuns
            RuleFor(x => x.BankName)
                .NotEmpty().WithMessage("Bank name is required")
                .MaximumLength(50).WithMessage("Bank name must not exceed 50 characters");

            RuleFor(x => x.BankCode)
                .NotEmpty().WithMessage("Bank code is required")
                .Must(IsValidBankCode).WithMessage("Bank code must be 3 digits");

            RuleFor(x => x.AccountHolderName)
                .NotEmpty().WithMessage("Account holder name is required")
                .MaximumLength(100).WithMessage("Account holder name must not exceed 100 characters");

            RuleFor(x => x.DocumentNumber)
                .NotEmpty().WithMessage("Document number is required")
                .Must(_documentValidator.IsValidDocument).WithMessage("Invalid document number format");

            RuleFor(x => x.SellerId)
                .NotEmpty().WithMessage("Seller ID is required");

            // Validações específicas por tipo de conta
            When(x => x.AccountType == BankAccountType.TED, () =>
            {
                RuleFor(x => x.AccountNumber)
                    .NotEmpty().WithMessage("Account number is required for TED accounts")
                    .Must(IsValidAccountNumber).WithMessage("Invalid account number format");

                RuleFor(x => x.BranchNumber)
                    .NotEmpty().WithMessage("Branch number is required for TED accounts")
                    .Must(IsValidBranchNumber).WithMessage("Invalid branch number format");
            });

            When(x => x.AccountType == BankAccountType.PIX, () =>
            {
                RuleFor(x => x.PixKey)
                    .NotEmpty().WithMessage("PIX key is required for PIX accounts");

                RuleFor(x => x.PixKeyType)
                    .NotNull().WithMessage("PIX key type is required for PIX accounts");

                RuleFor(x => x)
                    .Must(dto => _pixKeyValidator.IsValidPixKey(dto.PixKey, dto.PixKeyType))
                    .WithMessage("Invalid PIX key format for the selected PIX key type");
            });
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
    }

    public class BankAccountUpdateDtoValidator : AbstractValidator<BankAccountUpdateDto>
    {
        private readonly IPixKeyValidator _pixKeyValidator;

        public BankAccountUpdateDtoValidator(IPixKeyValidator pixKeyValidator)
        {
            _pixKeyValidator = pixKeyValidator;

            // Validações opcionais mas com formato correto quando preenchidas
            When(x => !string.IsNullOrEmpty(x.BankName), () =>
            {
                RuleFor(x => x.BankName)
                    .MaximumLength(50).WithMessage("Bank name must not exceed 50 characters");
            });

            When(x => !string.IsNullOrEmpty(x.BankCode), () =>
            {
                RuleFor(x => x.BankCode)
                    .Must(code => code.Length == 3 && code.All(char.IsDigit))
                    .WithMessage("Bank code must be 3 digits");
            });

            When(x => !string.IsNullOrEmpty(x.AccountHolderName), () =>
            {
                RuleFor(x => x.AccountHolderName)
                    .MaximumLength(100).WithMessage("Account holder name must not exceed 100 characters");
            });

            // Validações específicas por tipo de conta quando campos são preenchidos
            When(x => x.AccountType == BankAccountType.TED && !string.IsNullOrEmpty(x.AccountNumber), () =>
            {
                RuleFor(x => x.AccountNumber)
                    .Must(accountNumber => accountNumber.Length <= 20 && Regex.IsMatch(accountNumber, @"^\d+(-[\dxX])?$"))
                    .WithMessage("Invalid account number format");
            });

            When(x => x.AccountType == BankAccountType.TED && !string.IsNullOrEmpty(x.BranchNumber), () =>
            {
                RuleFor(x => x.BranchNumber)
                    .Must(branchNumber => branchNumber.Length <= 10 && Regex.IsMatch(branchNumber, @"^\d+(-[\dxX])?$"))
                    .WithMessage("Invalid branch number format");
            });

            When(x => x.AccountType == BankAccountType.PIX && !string.IsNullOrEmpty(x.PixKey), () =>
            {
                RuleFor(x => x.PixKeyType)
                    .NotNull().WithMessage("PIX key type is required when providing a PIX key");

                RuleFor(x => x)
                    .Must(dto => _pixKeyValidator.IsValidPixKey(dto.PixKey, dto.PixKeyType))
                    .WithMessage("Invalid PIX key format for the selected PIX key type");
            });
        }
    }

    public interface IDocumentValidator
    {
        bool IsValidDocument(string document);
        bool IsCpf(string cpf);
        bool IsCnpj(string cnpj);
    }

    public interface IPixKeyValidator
    {
        bool IsValidPixKey(string pixKey, PixKeyType? pixKeyType);
        bool IsValidEmail(string email);
        bool IsValidPhone(string phone);
        bool IsValidRandomKey(string key);
    }

    public class DocumentValidator : IDocumentValidator
    {
        public bool IsValidDocument(string document)
        {
            if (string.IsNullOrEmpty(document))
                return false;

            document = new string(document.Where(char.IsDigit).ToArray());

            return document.Length == 11 ? IsCpf(document) : IsCnpj(document);
        }

        public bool IsCpf(string cpf)
        {
            if (string.IsNullOrEmpty(cpf) || cpf.Length != 11)
                return false;

            // Elimina CPFs inválidos conhecidos como 00000000000, 11111111111, etc.
            if (cpf.Distinct().Count() == 1)
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

        public bool IsCnpj(string cnpj)
        {
            if (string.IsNullOrEmpty(cnpj) || cnpj.Length != 14)
                return false;

            // Elimina CNPJs inválidos conhecidos como 00000000000000, 11111111111111, etc.
            if (cnpj.Distinct().Count() == 1)
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
    }

    public class PixKeyValidator : IPixKeyValidator
    {
        private readonly IDocumentValidator _documentValidator;

        public PixKeyValidator(IDocumentValidator documentValidator)
        {
            _documentValidator = documentValidator;
        }

        public bool IsValidPixKey(string pixKey, PixKeyType? pixKeyType)
        {
            if (string.IsNullOrEmpty(pixKey) || !pixKeyType.HasValue)
                return false;

            return pixKeyType switch
            {
                PixKeyType.CPF => _documentValidator.IsCpf(pixKey),
                PixKeyType.CNPJ => _documentValidator.IsCnpj(pixKey),
                PixKeyType.EMAIL => IsValidEmail(pixKey),
                PixKeyType.PHONE => IsValidPhone(pixKey),
                PixKeyType.RANDOM => IsValidRandomKey(pixKey),
                _ => false
            };
        }

        public bool IsValidEmail(string email)
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

        public bool IsValidPhone(string phone)
        {
            // Formato: +55DDD999999999
            return Regex.IsMatch(phone, @"^\+55\d{11}$");
        }

        public bool IsValidRandomKey(string key)
        {
            // Chave aleatória do PIX tem 32 caracteres
            return key.Length == 32 &&
                   Regex.IsMatch(key, @"^[a-zA-Z0-9]+$");
        }
    }
}