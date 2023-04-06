using System.Collections.Concurrent;

namespace SampleDotnet.RepositoryFactory;

internal class UnitOfWork : IUnitOfWork
{
    private readonly Queue<DbContext> dbContextPool = new();
    private readonly ConcurrentDictionary<DbContextId, IRepository> repositoryPool = new();
    private readonly SemaphoreSlim semaphoreSlim = new(1, 1);
    private readonly IServiceProvider serviceProvider;
    private bool disposedValue;

    public UnitOfWork(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    public bool IsDbConcurrencyExceptionThrown { get => SaveChangesException != null && SaveChangesException.Exception is DbUpdateConcurrencyException; }

    public SaveChangesExceptionDetail? SaveChangesException { get; private set; }

    public IRepository<TDbContext> CreateRepository<TDbContext>(System.Transactions.TransactionScopeOption transactionScopeOption = System.Transactions.TransactionScopeOption.Required, System.Transactions.IsolationLevel isolationLevel = System.Transactions.IsolationLevel.ReadCommitted)
        where TDbContext : DbContext
    {
        var dbContext = serviceProvider
            .GetRequiredService<IDbContextFactory<TDbContext>>()
            .CreateDbContext();

        dbContextPool.Enqueue(dbContext);

        var repository = new Repository<TDbContext>(dbContext, transactionScopeOption, isolationLevel);
        repositoryPool.TryAdd(dbContext.ContextId, repository);
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
            await semaphoreSlim.WaitAsync(cancellationToken);

            int count = dbContextPool.Count;
            foreach (var dbContext in dbContextPool)
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
                    foreach (var context in dbContextPool)
                    {
                        await context.RollbackChangesAsync(false, cancellationToken);
                    }
                    throw;
                }
            }

            for (int i = 0; i < count; i++)
            {
                if (dbContextPool.TryDequeue(out var dbContext) && dbContext != null)
                {
                    dbContext.SilentDbContextDispose(false/*try true if required*/);
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
            semaphoreSlim.Release();
        }
        return true;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                foreach (var key in repositoryPool.Keys)
                {
                    if (repositoryPool.TryRemove(key, out var repository))
                    {
                        try { repository.Dispose(); } catch { }
                    }
                }
                while (dbContextPool.TryDequeue(out var dbContext) && dbContext != null)
                    dbContext.SilentDbContextDispose();

                semaphoreSlim.Dispose();
            }
            disposedValue = true;
        }
    }
}