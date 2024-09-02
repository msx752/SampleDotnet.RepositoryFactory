namespace SampleDotnet.RepositoryFactory;

/// <summary>
/// Unit of Work class to manage multiple DbContexts and repositories.
/// Implements the <see cref="IUnitOfWork"/> interface.
/// </summary>
internal class UnitOfWork : IUnitOfWork
{
    // A pool to hold created DbContexts for the lifetime of the UnitOfWork.
    private readonly Queue<DbContext> _dbContextPool = new();
    private readonly object _lock__dbContextPool = new object();

    // A thread-safe dictionary to manage repositories keyed by DbContextId.
    private readonly ConcurrentDictionary<DbContextId, IRepository> _repositoryPool = new();

    // SemaphoreSlim to handle concurrent save changes operations.
    private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);

    // The service provider to resolve DbContext factories and other dependencies.
    private readonly IServiceProvider _serviceProvider;

    // Indicates whether the object has been disposed.
    private bool disposedValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnitOfWork"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve dependencies.</param>
    public UnitOfWork(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Gets a value indicating whether a concurrency exception has been thrown during save operations.
    /// </summary>
    public bool IsDbConcurrencyExceptionThrown => SaveChangesException != null && SaveChangesException.Exception is DbUpdateConcurrencyException;

    /// <summary>
    /// Gets the details of the exception thrown during the save operation, if any.
    /// </summary>
    public SaveChangesExceptionDetail? SaveChangesException { get; private set; }

    /// <summary>
    /// Creates a repository for a specific <see cref="DbContext"/> type.
    /// </summary>
    /// <typeparam name="TDbContext">The type of DbContext.</typeparam>
    /// <returns>A repository instance for the specified DbContext.</returns>
    public IRepository<TDbContext> CreateRepository<TDbContext>()
        where TDbContext : DbContext
    {
        lock (_lock__dbContextPool)
        {
            var dbContext = _serviceProvider
                .GetRequiredService<IDbContextFactory<TDbContext>>()
                .CreateDbContext();

            // Enqueue the DbContext into the pool.
            _dbContextPool.Enqueue(dbContext);

            var repository = new Repository<TDbContext>(dbContext);

            // Add the repository to the pool, keyed by the DbContextId.
            _repositoryPool.TryAdd(dbContext.ContextId, repository);

            return repository;
        }
    }

    /// <summary>
    /// Disposes the UnitOfWork and releases all resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Saves all changes made in the context to the database synchronously.
    /// </summary>
    /// <returns>A boolean indicating success or failure.</returns>
    public bool SaveChanges()
    {
        // Synchronously wait for the asynchronous SaveChanges method to complete.
        return SaveChangesAsync().ConfigureAwait(false).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Saves all changes made in the context to the database asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous operation, with a boolean result indicating success or failure.</returns>
    public Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run(async () =>
        {
            DbContext? thrownExceptionDbContext = null;

            // Wait asynchronously to ensure only one save operation is performed at a time.
            await _semaphoreSlim.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                var cached = _dbContextPool.ToArray();
                var successfullyCommitedConnectionCount = 0;

                foreach (var dbContext in cached)
                {
                    try
                    {
                        // Detect changes if AutoDetectChanges is disabled.
                        if (!dbContext.ChangeTracker.AutoDetectChangesEnabled)
                            dbContext.ChangeTracker.DetectChanges();

                        // Save changes if there are any tracked changes.
                        if (dbContext.ChangeTracker.HasChanges())
                            await dbContext.SaveChangesAsync(false, cancellationToken).ConfigureAwait(false);

                        successfullyCommitedConnectionCount++;
                    }
                    catch
                    {
                        // Handle exception and rollback changes.
                        thrownExceptionDbContext = dbContext;

                        try
                        {
                            var orderedCached = cached.Take(successfullyCommitedConnectionCount)
                                                      .GroupBy(f => f.ToString())
                                                      .ToList();

                            // Rollback changes in parallel for successfully committed connections.
                            Parallel.ForEach(orderedCached, new ParallelOptions { MaxDegreeOfParallelism = 3, CancellationToken = cancellationToken }, cache =>
                            {
                                Parallel.For(0, cache.Count(), new ParallelOptions { MaxDegreeOfParallelism = 1, CancellationToken = cancellationToken }, async i =>
                                {
                                    await cache.ElementAt(i).RollbackChangesAsync(false, cancellationToken).ConfigureAwait(false);
                                });
                            });

                            cached[successfullyCommitedConnectionCount].ChangeTracker.Clear();
                        }
                        catch (Exception e)
                        {
                            throw new RollbackException(e.Message, e);
                        }

                        throw;
                    }
                }

                // Accept all changes for each DbContext after all are successfully committed.
                foreach (var dbContext in cached)
                {
                    dbContext.ChangeTracker.AcceptAllChanges();
                }
            }
            catch (RollbackException e)
            {
                throw new RollbackException("Rollback halt and recovery did not succeed; couldn't revert to the previous version, this is a critical problem!", e);
            }
            catch (Exception e) when (e is DbUpdateConcurrencyException || e is DbUpdateException)
            {
                SaveChangesException ??= new SaveChangesExceptionDetail(thrownExceptionDbContext, e);
                throw;
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                // Release the semaphore.
                _semaphoreSlim.Release();
            }

            return true;
        });
    }

    /// <summary>
    /// Disposes the managed resources used by the UnitOfWork.
    /// </summary>
    /// <param name="disposing">Indicates whether the method is being called by Dispose() or a finalizer.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // Dispose all Repositories
                foreach (var key in _repositoryPool.Keys)
                {
                    if (_repositoryPool.TryRemove(key, out var repository))
                        repository.Dispose();
                }

                // Dispose all DbContexts.
                while (_dbContextPool.TryDequeue(out var dbContext) && dbContext != null)
                    dbContext.Dispose();

                // Dispose the semaphore.
                _semaphoreSlim.Dispose();
            }

            disposedValue = true;
        }
    }
}