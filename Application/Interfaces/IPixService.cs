using Application.DTOs;

namespace Application.Interfaces
{
    public interface IPixService
    {
        Task<PixKeyValidationDto> ValidatePixKeyAsync(string pixKey, string pixKeyType);
        Task<PixPaymentConfirmationDto> ConfirmPixPaymentAsync(string validationId, decimal value);
    }
}
