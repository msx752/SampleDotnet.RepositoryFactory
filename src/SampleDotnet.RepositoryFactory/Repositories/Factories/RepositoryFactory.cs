namespace SampleDotnet.RepositoryFactory.Repositories.Factories;

/// <summary>
/// Provides a factory for creating and managing repository instances.
/// Implements the <see cref="IRepositoryFactory"/> interface.
/// </summary>
public class RepositoryFactory : IRepositoryFactory, IDisposable
{
    private readonly ConcurrentDictionary<DbContextId, IRepository> _repositoryPool = new();
    private bool _disposedValue;

    /// <summary>
    /// Creates a new repository instance for the specified DbContext and adds it to the repository pool.
    /// </summary>
    /// <typeparam name="TDbContext">The type of the DbContext for which the repository is created.</typeparam>
    /// <param name="dbContext">The DbContext instance used to create the repository.</param>
    /// <returns>An instance of a repository for the specified DbContext type.</returns>
    public IRepository<TDbContext> CreateRepository<TDbContext>(TDbContext dbContext) where TDbContext : DbContext
    {
        var repository = new Repository<TDbContext>(dbContext);
        _repositoryPool.TryAdd(dbContext.ContextId, repository);
        return repository;
    }

    /// <summary>
    /// Disposes of the resources used by the repository factory, including all repositories in the pool.
    /// </summary>
    /// <param name="disposing">Indicates whether the method is being called by Dispose() or a finalizer.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                // Dispose all repositories in the pool.
                foreach (var key in _repositoryPool.Keys)
                {
                    if (_repositoryPool.TryRemove(key, out var repository))
                    {
                        repository.Dispose();
                    }
                }
            }

            _disposedValue = true;
        }
    }

    /// <summary>
    /// Disposes the repository factory and releases all resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}