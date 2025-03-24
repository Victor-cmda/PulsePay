using Application.DTOs;

namespace Application.Interfaces
{
    public interface IDepositService
    {
        Task<DepositDto> CreateDepositRequestAsync(DepositRequestDto request);
        Task<DepositDto> GetDepositAsync(Guid id);
        Task<IEnumerable<DepositDto>> GetDepositsBySellerIdAsync(Guid sellerId, int page = 1, int pageSize = 20);
        Task<DepositDto> ProcessDepositCallbackAsync(string transactionId, string status, decimal amount);
    }
}
