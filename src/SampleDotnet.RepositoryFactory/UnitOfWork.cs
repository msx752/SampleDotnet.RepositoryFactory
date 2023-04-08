using System.Collections.Concurrent;

namespace SampleDotnet.RepositoryFactory;

internal class UnitOfWork : IUnitOfWork
{
    private readonly Queue<DbContext> _dbContextPool = new();
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

    public IRepository<TDbContext> CreateRepository<TDbContext>(/*System.Transactions.TransactionScopeOption transactionScopeOption = System.Transactions.TransactionScopeOption.RequiresNew, System.Transactions.IsolationLevel isolationLevel = System.Transactions.IsolationLevel.ReadCommitted*/)
        where TDbContext : DbContext
    {
        var dbContext = _serviceProvider
            .GetRequiredService<IDbContextFactory<TDbContext>>()
            .CreateDbContext();

        _dbContextPool.Enqueue(dbContext);

        var repository = new Repository<TDbContext>(dbContext/*, transactionScopeOption, isolationLevel*/);
        _repositoryPool.TryAdd(dbContext.ContextId, repository);
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
        await _semaphoreSlim.WaitAsync(cancellationToken);
        try
        {
            var cached = _dbContextPool.ToArray();
            foreach (var dbContext in cached)
            {
                try
                {
                    if (!dbContext.ChangeTracker.AutoDetectChangesEnabled)
                        dbContext.ChangeTracker.DetectChanges();
                    await dbContext.SaveChangesAsync(false, cancellationToken);
                }
                catch
                {
                    thrownExceptionDbContext = dbContext;
                    foreach (var context in cached)
                    {
                        await context.RollbackChangesAsync(false, cancellationToken);
                    }
                    throw;
                }
            }

            for (int i = 0; i < cached.Length; i++)
            {
                if (_dbContextPool.TryDequeue(out var dbContext) && dbContext != null)
                {
                    dbContext.ChangeTracker.AcceptAllChanges();
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

                while (_dbContextPool.TryDequeue(out var dbContext) && dbContext != null)
                {
                    try { dbContext.Dispose(); } catch { }
                }

                _semaphoreSlim.Dispose();
            }
            disposedValue = true;
        }
    }
}