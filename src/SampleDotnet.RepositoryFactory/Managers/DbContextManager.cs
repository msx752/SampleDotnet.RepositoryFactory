namespace SampleDotnet.RepositoryFactory.Database;

/// <summary>
/// Manages the lifecycle of DbContext instances, including creation and disposal.
/// Implements the <see cref="IDbContextManager"/> interface.
/// </summary>
public class DbContextManager : IDbContextManager, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Queue<DbContext> _dbContextPool = new();
    private bool _disposedValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="DbContextManager"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for resolving dependencies.</param>
    public DbContextManager(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Creates and returns a new instance of the specified DbContext type.
    /// </summary>
    /// <typeparam name="TDbContext">The type of the DbContext to create.</typeparam>
    /// <returns>A new instance of the specified DbContext type.</returns>
    public TDbContext CreateDbContext<TDbContext>() where TDbContext : DbContext
    {
        var dbContext = _serviceProvider
            .GetRequiredService<IDbContextFactory<TDbContext>>()
            .CreateDbContext();

        _dbContextPool.Enqueue(dbContext);
        return dbContext;
    }

    /// <summary>
    /// Returns an array of all cached DbContext instances created by this manager.
    /// </summary>
    /// <returns>An array of DbContext instances.</returns>
    public DbContext[] CachedDbContexts() => _dbContextPool.ToArray();

    /// <summary>
    /// Disposes of the managed resources, including all DbContext instances in the pool.
    /// </summary>
    /// <param name="disposing">Indicates whether the method is being called by Dispose() or a finalizer.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                // Dispose of all cached DbContext instances.
                while (_dbContextPool.TryDequeue(out var dbContext))
                {
                    dbContext.Dispose();
                }
            }

            _disposedValue = true;
        }
    }

    /// <summary>
    /// Disposes the DbContextManager and releases all resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}