using Application.DTOs;

namespace Application.Interfaces
{
    public interface IRefundService
    {
        Task<RefundResponseDto> RequestRefundAsync(RefundRequestDto request, Guid sellerId);
        Task<RefundResponseDto> GetRefundStatusAsync(Guid id);
        Task<IEnumerable<RefundResponseDto>> GetRefundsBySellerIdAsync(Guid sellerId, int page = 1, int pageSize = 20);
        Task<IEnumerable<RefundResponseDto>> GetPendingRefundsAsync(int page = 1, int pageSize = 20);

        Task<RefundResponseDto> ApproveRefundAsync(Guid id, string adminId);
        Task<RefundResponseDto> RejectRefundAsync(Guid id, string reason, string adminId);
        Task<RefundResponseDto> CompleteRefundAsync(Guid id, string transactionReceipt);
    }
}
