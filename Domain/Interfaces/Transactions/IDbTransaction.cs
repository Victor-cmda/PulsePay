using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Interfaces.Transactions
{
    /// <summary>
    /// Interface para gerenciar transações de banco de dados
    /// </summary>
    public interface IDbTransaction : IAsyncDisposable
    {
        Task CommitAsync();
        Task RollbackAsync();
    }
}
