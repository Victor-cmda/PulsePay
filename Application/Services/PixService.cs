using Application.DTOs;
using Application.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace Application.Services
{
    public class PixService : IPixService
    {
        private readonly ILogger<PixService> _logger;

        public PixService(ILogger<PixService> logger)
        {
            _logger = logger;
        }

        public Task<PixKeyValidationDto> ValidatePixKeyAsync(string pixKey, string pixKeyType)
        {
            var isValid = false;
            var errorMessage = string.Empty;

            pixKeyType = pixKeyType.ToUpper();

            switch (pixKeyType)
            {
                case "CPF":
                    isValid = ValidateCpf(pixKey);
                    if (!isValid) errorMessage = "CPF inválido ou com formato incorreto";
                    break;

                case "CNPJ":
                    isValid = ValidateCnpj(pixKey);
                    if (!isValid) errorMessage = "CNPJ inválido ou com formato incorreto";
                    break;

                case "EMAIL":
                    isValid = ValidateEmail(pixKey);
                    if (!isValid) errorMessage = "Email inválido ou com formato incorreto";
                    break;

                case "PHONE":
                    isValid = ValidatePhone(pixKey);
                    if (!isValid) errorMessage = "Telefone inválido ou com formato incorreto";
                    break;

                case "RANDOM":
                    isValid = ValidateRandomKey(pixKey);
                    if (!isValid) errorMessage = "Chave aleatória inválida ou com formato incorreto";
                    break;

                default:
                    errorMessage = "Tipo de chave PIX não suportado";
                    break;
            }

            _logger.LogInformation(
                "Validação de chave PIX: {PixKey}, Tipo: {PixKeyType}, Resultado: {IsValid}",
                pixKey, pixKeyType, isValid);

            string validationId = isValid ?
                GenerateValidationId(pixKey, pixKeyType) :
                null;

            var result = new PixKeyValidationDto
            {
                IsValid = isValid,
                PixKey = pixKey,
                PixKeyType = pixKeyType,
                ValidationId = validationId,
                ErrorMessage = isValid ? null : errorMessage,
                ValidatedAt = DateTime.UtcNow
            };

            return Task.FromResult(result);
        }

        public Task<PixPaymentConfirmationDto> ConfirmPixPaymentAsync(string validationId, decimal value)
        {
            _logger.LogInformation(
                "Confirmando pagamento PIX: ValidaçãoID: {ValidationId}, Valor: {Value}",
                validationId, value);

            string paymentId = Guid.NewGuid().ToString();

            var result = new PixPaymentConfirmationDto
            {
                Success = true,
                PaymentId = paymentId,
                PaymentProofId = $"PROOF-{paymentId}",
                ErrorMessage = null
            };

            return Task.FromResult(result);
        }

        #region Métodos de Validação

        private bool ValidateCpf(string cpf)
        {
            cpf = Regex.Replace(cpf, @"[^\d]", "");

            if (cpf.Length != 11)
                return false;

            bool allEqual = true;
            for (int i = 1; i < cpf.Length; i++)
            {
                if (cpf[i] != cpf[0])
                {
                    allEqual = false;
                    break;
                }
            }
            if (allEqual)
                return false;

            return true;
        }

        private bool ValidateCnpj(string cnpj)
        {
            cnpj = Regex.Replace(cnpj, @"[^\d]", "");

            if (cnpj.Length != 14)
                return false;

            bool allEqual = true;
            for (int i = 1; i < cnpj.Length; i++)
            {
                if (cnpj[i] != cnpj[0])
                {
                    allEqual = false;
                    break;
                }
            }
            if (allEqual)
                return false;

            return true;
        }

        private bool ValidateEmail(string email)
        {
            var emailPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
            return Regex.IsMatch(email, emailPattern);
        }

        private bool ValidatePhone(string phone)
        {
            var phonePattern = @"^\+55\d{10,11}$";
            return Regex.IsMatch(phone, phonePattern);
        }

        private bool ValidateRandomKey(string key)
        {
            var randomKeyPattern = @"^[a-zA-Z0-9]{32}$";
            return Regex.IsMatch(key, randomKeyPattern);
        }

        private string GenerateValidationId(string pixKey, string pixKeyType)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var input = $"{pixKey}:{pixKeyType}:{timestamp}";

            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                var sb = new System.Text.StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }

        #endregion
    }
}
