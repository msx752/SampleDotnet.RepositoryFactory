namespace SampleDotnet.RepositoryFactory;

/// <summary>
/// Manages transactions across multiple DbContext instances, ensuring consistency and providing rollback functionality.
/// Implements the <see cref="ITransactionManager"/> interface.
/// </summary>
public class TransactionManager : ITransactionManager, IDisposable
{
    private readonly IDbContextManager _dbContextManager;
    private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);
    private bool _disposedValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionManager"/> class.
    /// </summary>
    /// <param name="dbContextManager">The DbContext manager to manage DbContext instances.</param>
    public TransactionManager(IDbContextManager dbContextManager)
    {
        _dbContextManager = dbContextManager;
    }

    /// <summary>
    /// Gets a value indicating whether a DbConcurrencyException has been thrown.
    /// </summary>
    public bool IsDbConcurrencyExceptionThrown => SaveChangesException != null && SaveChangesException.Exception is DbUpdateConcurrencyException;

    /// <summary>
    /// Gets the details of the exception thrown during SaveChanges operations, if any.
    /// </summary>
    public SaveChangesExceptionDetail? SaveChangesException { get; private set; }

    /// <summary>
    /// Asynchronously saves changes in all cached DbContext instances managed by this TransactionManager.
    /// If any exception occurs, all changes are rolled back.
    /// </summary>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous operation, with a boolean indicating success or failure.</returns>
    public async Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        DbContext? thrownExceptionDbContext = null;

        await _semaphoreSlim.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var cached = _dbContextManager.CachedDbContexts();
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
                        await dbContext.SaveChangesAsync(false, cancellationToken);

                    successfullyCommitedConnectionCount++;
                }
                catch
                {
                    // Handle exception and rollback changes.
                    thrownExceptionDbContext = dbContext;

                    await RollbackCommittedContextsAsync(cached, successfullyCommitedConnectionCount, cancellationToken).ConfigureAwait(false);

                    throw;
                }
            }

            // Accept all changes after successful commits.
            AcceptAllChanges(cached);
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
            _semaphoreSlim.Release();
        }

        return true;
    }

    /// <summary>
    /// Asynchronously rolls back changes in the specified DbContext instance to revert to the previous state.
    /// </summary>
    /// <param name="context">The DbContext instance to rollback.</param>
    /// <param name="overrideDetectChanges">Indicates whether to override change detection settings.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task RollbackChangesAsync(DbContext context, bool overrideDetectChanges = false, CancellationToken cancellationToken = default)
    {
        // Detect changes manually if override is enabled and AutoDetectChanges is off.
        if (overrideDetectChanges && !context.ChangeTracker.AutoDetectChangesEnabled)
            context.ChangeTracker.DetectChanges();

        // If there are changes to be rolled back.
        if (context.ChangeTracker.HasChanges())
        {
            // Iterate over all tracked entries and rollback state changes.
            foreach (var entry in context.ChangeTracker.Entries().Where(e => e.State != EntityState.Unchanged && e.State != EntityState.Detached))
            {
                switch (entry.State)
                {
                    case EntityState.Modified:
                        // Revert modified properties to their original values.
                        foreach (string propertyName in entry.OriginalValues.Properties.Select(f => f.Name))
                        {
                            entry.Property(propertyName).CurrentValue = entry.Property(propertyName).OriginalValue;
                        }
                        break;

                    case EntityState.Deleted:
                        // Change the state to Modified and then to Unchanged to cancel deletion.
                        entry.State = EntityState.Modified;
                        entry.State = EntityState.Unchanged;
                        break;

                    case EntityState.Added:
                        // Change the state to Deleted to remove newly added entities.
                        entry.State = EntityState.Deleted;
                        break;
                }
            }

            try
            {
                // Attempt to save the changes, effectively rolling back to the previous state.
                await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                // Ignored. This exception is expected if the SaveChangesAsync call fails due to concurrency conflicts.
                // These entities have already been validated with the previous SaveChangesAsync call,
                // so this error occurs when attempting to save changes again.
            }
            catch (Exception)
            {
                // Re-throw any other exceptions.
                throw;
            }
        }
    }

    /// <summary>
    /// Rolls back changes in all DbContext instances that have successfully committed changes.
    /// </summary>
    /// <param name="cached">The array of cached DbContext instances.</param>
    /// <param name="successfullyCommitedConnectionCount">The count of successfully committed DbContext instances.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task RollbackCommittedContextsAsync(DbContext[] cached, int successfullyCommitedConnectionCount, CancellationToken cancellationToken)
    {
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
                    await RollbackChangesAsync(cache.ElementAt(i), false, cancellationToken).ConfigureAwait(false);
                });
            });

            cached[successfullyCommitedConnectionCount].ChangeTracker.Clear();
        }
        catch (Exception e)
        {
            throw new RollbackException(e.Message, e);
        }
    }

    /// <summary>
    /// Accepts all changes in the given DbContext instances after successful commits.
    /// </summary>
    /// <param name="cached">The array of cached DbContext instances.</param>
    private void AcceptAllChanges(DbContext[] cached)
    {
        foreach (var dbContext in cached)
        {
            dbContext.ChangeTracker.AcceptAllChanges();
        }
    }

    /// <summary>
    /// Disposes of the managed resources, including the DbContext manager and semaphore.
    /// </summary>
    /// <param name="disposing">Indicates whether the method is being called by Dispose() or a finalizer.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _dbContextManager?.Dispose();
                _semaphoreSlim?.Dispose();
            }

            _disposedValue = true;
        }
    }

    /// <summary>
    /// Disposes the TransactionManager and releases all resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}