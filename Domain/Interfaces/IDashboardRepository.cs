using Domain.Models;

namespace Domain.Interfaces
{
    public interface IDashboardRepository
    {
        Task<IEnumerable<Transaction>> GetTransactionsBySellers(List<Guid> sellers);
    }
}
