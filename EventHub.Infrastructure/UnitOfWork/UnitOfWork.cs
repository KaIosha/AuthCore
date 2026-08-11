using EventHub.Application.Interfaces;
using EventHub.Infrastructure.Data;
using EventHub.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace EventHub.Infrastructure.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private readonly Dictionary<Type, object> _repositories = new();

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public IGenericRepository<T> Repository<T>() where T : class
    {
        var type = typeof(T);

        if (!_repositories.TryGetValue(type, out var repository))
        {
            repository = new GenericRepository<T>(_context);
            _repositories[type] = repository;
        }

        return (IGenericRepository<T>)repository;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);

    // Begins a DB transaction on the SAME scoped context that Identity uses, so
    // UserManager's internal saves and your UoW saves all join the same transaction.
    // The caller does: await using var tx = await uow.BeginTransactionAsync();
    // then tx.CommitAsync() on success (disposal auto-rolls-back on exception).
    // Returns the EF transaction wrapped in an ITransaction so the Application
    // layer never touches EF Core types.
    public async Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        var dbTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        return new Transaction(dbTransaction);
    }

    public void Dispose() => _context.Dispose();

    // Wraps the EF Core transaction behind the Application-layer ITransaction contract.
    private sealed class Transaction : ITransaction
    {
        private readonly IDbContextTransaction _dbTransaction;

        public Transaction(IDbContextTransaction dbTransaction)
        {
            _dbTransaction = dbTransaction;
        }

        public Task CommitAsync(CancellationToken cancellationToken = default)
            => _dbTransaction.CommitAsync(cancellationToken);

        public ValueTask DisposeAsync() => _dbTransaction.DisposeAsync();
    }
}
