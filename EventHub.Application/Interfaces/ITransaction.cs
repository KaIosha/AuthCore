namespace EventHub.Application.Interfaces;

// Keeps EF Core OUT of the Application layer:
// the Application only knows how to commit/dispose a transaction,
// never the EF-specific IDbContextTransaction type.
public interface ITransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
}
