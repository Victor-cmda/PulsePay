using Application.DTOs;

namespace Application.Interfaces
{
    public interface IWithdrawService
    {
        Task<WithdrawDto> RequestWithdrawAsync(WithdrawRequestDto request);
        Task<WithdrawDto> GetWithdrawAsync(Guid id);
        Task<IEnumerable<WithdrawDto>> GetWithdrawsBySellerIdAsync(Guid sellerId, int page = 1, int pageSize = 20);
        Task<IEnumerable<WithdrawDto>> GetPendingWithdrawsAsync(int page = 1, int pageSize = 20);
        Task<WithdrawDto> ApproveWithdrawAsync(Guid id, string adminId);
        Task<WithdrawDto> RejectWithdrawAsync(Guid id, string reason, string adminId);
        Task<WithdrawDto> ProcessWithdrawAsync(Guid id, string transactionReceipt);
        Task<int> GetPendingWithdrawsCountAsync();
        Task<decimal> GetTotalPendingWithdrawAmountAsync();
    }
}
