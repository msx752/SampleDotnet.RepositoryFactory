using System.Collections.Concurrent;

namespace SampleDotnet.RepositoryFactory;

internal class UnitOfWork : IUnitOfWork
{
    private readonly Queue<DbContextId> _dbContextQueue = new();
    private readonly ConcurrentDictionary<DbContextId, IRepository> _repositoryPool = new();
    private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);
    private readonly IServiceProvider _serviceProvider;
    private bool disposedValue;

    public UnitOfWork(IServiceProvider serviceProvider)
    {
        this._serviceProvider = serviceProvider;
    }

    public bool IsDbConcurrencyExceptionThrown { get => SaveChangesException != null && SaveChangesException.Exception is DbUpdateConcurrencyException; }

    public SaveChangesExceptionDetail? SaveChangesException { get; private set; }

    public IRepository<TDbContext> CreateRepository<TDbContext>(System.Transactions.TransactionScopeOption transactionScopeOption = System.Transactions.TransactionScopeOption.Required, System.Transactions.IsolationLevel isolationLevel = System.Transactions.IsolationLevel.ReadCommitted)
        where TDbContext : DbContext
    {
        var dbContext = _serviceProvider
            .GetRequiredService<IDbContextFactory<TDbContext>>();

        var repository = new Repository<TDbContext>(dbContext, transactionScopeOption, isolationLevel);
        _dbContextQueue.Enqueue(repository.DbContext.ContextId);
        _repositoryPool.TryAdd(repository.DbContext.ContextId, repository);
        return repository;
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public bool SaveChanges()
    {
        return this.SaveChangesAsync().ConfigureAwait(false).GetAwaiter().GetResult();
    }

    public async Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        DbContext? thrownExceptionDbContext = null;
        try
        {
            await _semaphoreSlim.WaitAsync(cancellationToken);

            int count = _dbContextQueue.Count;
            foreach (var dbContextKey in _dbContextQueue)
            {
                if (_repositoryPool.TryGetValue(dbContextKey, out var repo))
                {
                    try
                    {
                        if (!repo.ChangeTracker.AutoDetectChangesEnabled)
                            repo.ChangeTracker.DetectChanges();
                        await repo.DbContext.SaveChangesAsync(false, cancellationToken);
                    }
                    catch
                    {
                        thrownExceptionDbContext = repo.DbContext;

                        foreach (var dbContextKey2 in _dbContextQueue)
                        {
                            if (_repositoryPool.TryGetValue(dbContextKey2, out var repo2))
                            {
                                await repo2.DbContext.RollbackChangesAsync(false, cancellationToken);
                            }
                        }
                        throw;
                    }
                }
            }

            for (int i = 0; i < count; i++)
            {
                if (_dbContextQueue.TryDequeue(out var dbContextKey) && dbContextKey != null)
                {
                    if (_repositoryPool.TryRemove(dbContextKey, out var repo))
                    {
                        var newDbContext = repo.RefreshDbContext();
                        _dbContextQueue.Enqueue(newDbContext.ContextId);
                        _repositoryPool.TryAdd(newDbContext.ContextId, repo);
                    }
                }
            }
        }
        catch (Exception e)
        {
            SaveChangesException ??= new(thrownExceptionDbContext, e);
            throw;
        }
        finally
        {
            _semaphoreSlim.Release();
        }
        return true;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                foreach (var key in _repositoryPool.Keys)
                {
                    if (_repositoryPool.TryRemove(key, out var repository))
                    {
                        try { repository.Dispose(); } catch { }
                    }
                }

                _dbContextQueue.Clear();
                _semaphoreSlim.Dispose();
            }
            disposedValue = true;
        }
    }
}