using Application.DTOs;

namespace Application.Interfaces
{
    public interface ICustomerPayoutService
    {
        Task<CustomerPayoutDto> RequestPayoutAsync(CustomerPayoutRequestDto request);
        Task<CustomerPayoutDto> GetPayoutAsync(Guid id, Guid sellerId);
        Task<IEnumerable<CustomerPayoutDto>> GetPayoutsBySellerIdAsync(Guid sellerId, int page = 1, int pageSize = 20);
        Task<IEnumerable<CustomerPayoutDto>> GetPendingPayoutsAsync(int page = 1, int pageSize = 20);
        Task<PixKeyValidationDto> ValidatePixKeyAsync(Guid payoutId);
        Task<CustomerPayoutDto> ConfirmPayoutAsync(Guid payoutId, decimal value, string proofReference, string adminId);
        Task<CustomerPayoutDto> RejectPayoutAsync(Guid payoutId, string reason, string adminId);
    }
}
