using Application.DTOs;
using Application.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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
            // Realizando validação básica de formato da chave PIX
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
                "Validação interna de chave PIX: {PixKey}, Tipo: {PixKeyType}, Resultado: {IsValid}",
                pixKey, pixKeyType, isValid);

            // Retorna resultado da validação
            var result = new PixKeyValidationDto
            {
                IsValid = isValid,
                PixKey = pixKey,
                PixKeyType = pixKeyType,
                KeyOwnerName = isValid ? "Nome não verificado (validação interna)" : null,
                KeyOwnerDocument = isValid ? "Documento não verificado (validação interna)" : null,
                BankName = isValid ? "Banco não verificado (validação interna)" : null,
                ValidationId = isValid ? Guid.NewGuid().ToString() : null,
                ErrorMessage = isValid ? null : errorMessage
            };

            return Task.FromResult(result);
        }

        public Task<PixPaymentConfirmationDto> RegisterManualPaymentAsync(
            Guid payoutId, decimal value, string proofReference, string adminId)
        {
            // Aqui apenas registramos que o administrador confirmou o pagamento manual
            // com alguma referência de comprovante
            _logger.LogInformation(
                "Pagamento manual registrado: {PayoutId}, Valor: {Value}, Referência: {Reference}, Admin: {AdminId}",
                payoutId, value, proofReference, adminId);

            var result = new PixPaymentConfirmationDto
            {
                Success = true,
                PaymentId = payoutId.ToString(),
                PaymentProofId = proofReference
            };

            return Task.FromResult(result);
        }

        #region Métodos de Validação

        private bool ValidateCpf(string cpf)
        {
            // Remove caracteres não numéricos
            cpf = Regex.Replace(cpf, @"[^\d]", "");

            // Verifica se tem 11 dígitos
            if (cpf.Length != 11)
                return false;

            // Verifica se todos os dígitos são iguais
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

            // Uma validação simplificada - em produção você deve implementar a validação completa
            return true;
        }

        private bool ValidateCnpj(string cnpj)
        {
            // Remove caracteres não numéricos
            cnpj = Regex.Replace(cnpj, @"[^\d]", "");

            // Verifica se tem 14 dígitos
            if (cnpj.Length != 14)
                return false;

            // Verifica se todos os dígitos são iguais
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

            // Uma validação simplificada - em produção você deve implementar a validação completa
            return true;
        }

        private bool ValidateEmail(string email)
        {
            // Regex para validação básica de email
            var emailPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
            return Regex.IsMatch(email, emailPattern);
        }

        private bool ValidatePhone(string phone)
        {
            // Valida telefone no formato +5511999999999
            var phonePattern = @"^\+55\d{10,11}$";
            return Regex.IsMatch(phone, phonePattern);
        }

        private bool ValidateRandomKey(string key)
        {
            // Chaves aleatórias do PIX possuem 32 caracteres alfanuméricos
            var randomKeyPattern = @"^[a-zA-Z0-9]{32}$";
            return Regex.IsMatch(key, randomKeyPattern);
        }

        #endregion
    }
}
