using Application.DTOs;

namespace Application.Interfaces
{
    public interface IPixService
    {
        Task<PixKeyValidationDto> ValidatePixKeyAsync(string pixKey, string correlationId);
        Task<PixPaymentConfirmationDto> ConfirmPixPaymentAsync(string paymentId, decimal value);
    }
}
