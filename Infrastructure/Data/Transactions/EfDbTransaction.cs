using Domain.Interfaces.Transactions;
using Microsoft.EntityFrameworkCore.Storage;

/// <summary>
/// Implementação de IDbTransaction usando Entity Framework Core
/// </summary>
public class EfDbTransaction : IDbTransaction
{
    private readonly IDbContextTransaction _transaction;

    public EfDbTransaction(IDbContextTransaction transaction)
    {
        _transaction = transaction;
    }

    public async Task CommitAsync()
    {
        await _transaction.CommitAsync();
    }

    public async Task RollbackAsync()
    {
        await _transaction.RollbackAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _transaction.DisposeAsync();
    }
}