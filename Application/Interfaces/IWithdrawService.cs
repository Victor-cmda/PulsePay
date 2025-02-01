using Application.DTOs;

namespace Application.Interfaces
{
    public interface IWithdrawService
    {
        Task<WithdrawResponseDto> RequestWithdrawAsync(WithdrawCreateDto createDto);
        Task<WithdrawResponseDto> GetWithdrawAsync(Guid id);
        Task<IEnumerable<WithdrawResponseDto>> GetWithdrawsBySellerAsync(Guid sellerId, int page = 1, int pageSize = 10);
        Task<WithdrawResponseDto> ProcessWithdrawAsync(Guid id, WithdrawUpdateDto updateDto);
        Task<WithdrawSummaryDto> GetWithdrawSummaryAsync(Guid sellerId, DateTime startDate, DateTime endDate);
        Task<IEnumerable<WithdrawResponseDto>> GetPendingWithdrawsAsync();
    }
}
