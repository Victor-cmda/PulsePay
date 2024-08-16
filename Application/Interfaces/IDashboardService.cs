using Application.DTOs;

namespace Application.Interfaces
{
    public interface IDashboardService
    {
        Task<DashboardDataDto> GetDashboard(List<Guid> sellers);
        Task<List<TransactionDataDto>> GetHistoryTransactions(List<Guid> sellers);
    }
}
