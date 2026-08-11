namespace EventHub.Application.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IGenericRepository<T> Repository<T>() where T : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    // Returns the transaction so the caller controls commit/rollback (await using).
    // Commit/Rollback live on the returned ITransaction, not here.
    Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
