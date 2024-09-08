namespace SampleDotnet.RepositoryFactory;

/// <summary>
/// Manages transactional operations across multiple repositories and DbContext instances.
/// Implements the <see cref="IUnitOfWork"/> interface to provide a single, consistent transaction scope.
/// </summary>
public class UnitOfWork : IUnitOfWork, IDisposable
{
    private readonly IDbContextManager _dbContextManager;
    private readonly IRepositoryFactory _repositoryFactory;
    private readonly ITransactionManager _transactionalManager;
    private bool disposedValue = false;
    private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);
    private readonly object _lock_CreateRepository = new object();

    /// <summary>
    /// Initializes a new instance of the <see cref="UnitOfWork"/> class.
    /// </summary>
    /// <param name="dbContextManager">The manager for creating and managing DbContext instances.</param>
    /// <param name="repositoryFactory">The factory for creating repository instances.</param>
    /// <param name="transactionalManager">The manager for handling transactions.</param>
    public UnitOfWork(IDbContextManager dbContextManager, IRepositoryFactory repositoryFactory, ITransactionManager transactionalManager)
    {
        _dbContextManager = dbContextManager;
        _repositoryFactory = repositoryFactory;
        _transactionalManager = transactionalManager;
    }

    /// <summary>
    /// Gets a value indicating whether a DbConcurrencyException has been thrown during a transaction.
    /// </summary>
    public bool IsDbConcurrencyExceptionThrown => _transactionalManager.IsDbConcurrencyExceptionThrown;

    /// <summary>
    /// Gets the details of the exception thrown during SaveChanges operations, if any.
    /// </summary>
    public SaveChangesExceptionDetail? SaveChangesException => _transactionalManager.SaveChangesException;

    /// <summary>
    /// Creates and returns a repository instance for the specified DbContext type.
    /// </summary>
    /// <typeparam name="TDbContext">The type of the DbContext for which the repository is created.</typeparam>
    /// <returns>An instance of a repository for the specified DbContext type.</returns>
    public IRepository<TDbContext> CreateRepository<TDbContext>() where TDbContext : DbContext
    {
        lock (_lock_CreateRepository)
        {
            var dbContext = _dbContextManager.CreateDbContext<TDbContext>();
            return _repositoryFactory.CreateRepository(dbContext);
        }
    }

    /// <summary>
    /// Saves all changes made in the current unit of work synchronously.
    /// </summary>
    /// <returns>true if the changes were successfully saved; otherwise, false.</returns>
    public bool SaveChanges()
    {
        return SaveChangesAsync().ConfigureAwait(false).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Asynchronously saves all changes made in the current unit of work.
    /// </summary>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous operation, with a boolean indicating success or failure.</returns>
    public async Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _transactionalManager.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Disposes of the resources used by the unit of work, including the DbContext manager, repository factory, and transaction manager.
    /// </summary>
    /// <param name="disposing">Indicates whether the method is being called by Dispose() or a finalizer.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                _dbContextManager.Dispose();
                _repositoryFactory.Dispose();
                _transactionalManager.Dispose();
                _semaphoreSlim.Dispose();
            }

            disposedValue = true;
        }
    }

    /// <summary>
    /// Disposes the unit of work and releases all resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}