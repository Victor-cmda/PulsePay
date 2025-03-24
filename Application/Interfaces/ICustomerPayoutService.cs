using Application.DTOs;

namespace Application.Interfaces
{
    public interface ICustomerPayoutService
    {
        Task<PixKeyValidationDto> ValidatePixKeyAsync(PixValidationRequestDto request);
        Task<CustomerPayoutResponseDto> CreatePayoutAsync(CustomerPayoutCreateDto request, Guid sellerId);
        Task<CustomerPayoutResponseDto> GetPayoutAsync(Guid id);
        Task<IEnumerable<CustomerPayoutResponseDto>> GetPayoutsBySellerIdAsync(Guid sellerId, int page = 1, int pageSize = 20);
        Task<IEnumerable<CustomerPayoutResponseDto>> GetPendingPayoutsAsync(int page = 1, int pageSize = 20);
        Task<CustomerPayoutResponseDto> ConfirmPayoutAsync(Guid payoutId, string paymentProofId, string adminId);
        Task<CustomerPayoutResponseDto> RejectPayoutAsync(Guid payoutId, string reason, string adminId);
    }
}