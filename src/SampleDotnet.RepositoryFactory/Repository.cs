namespace SampleDotnet.RepositoryFactory;

internal class Repository<TDbContext> : RepositoryBase
    , IRepository<TDbContext>
    where TDbContext : DbContext
{
    private static readonly Func<object, object> _funcCreatedAt = new((entity) =>
    {
        if (entity is IHasDateTimeOffset dt)
            dt.CreatedAt = DateTimeOffset.Now;
        return entity;
    });

    //private readonly TransactionOptions transactionOptions;
    //private readonly TransactionScopeOption transactionScopeOption;

    public Repository(TDbContext dbContext/*, TransactionScopeOption transactionScopeOption, System.Transactions.IsolationLevel isolationLevel*/)
        : base(dbContext)
    {
        //this.transactionScopeOption = transactionScopeOption;
        //this.transactionOptions = new() { IsolationLevel = isolationLevel };
    }

    public IQueryable<T> AsQueryable<T>() where T : class
    {
        return CachedDbSet<T>();
    }

    public void Delete<T>(T entity) where T : class
    {
        ArgumentNullException.ThrowIfNull(entity, nameof(entity));

        CachedDbSet<T>().Remove(entity);
    }

    public void Delete<T>(params T[] entities) where T : class
    {
        ArgumentNullException.ThrowIfNull(entities, nameof(entities));

        CachedDbSet<T>().RemoveRange(entities);
    }

    public void Delete<T>(IEnumerable<T> entities) where T : class
    {
        ArgumentNullException.ThrowIfNull(entities, nameof(entities));

        CachedDbSet<T>().RemoveRange(entities);
    }

    public T? Find<T>(params object[] keyValues) where T : class
    {
        ArgumentNullException.ThrowIfNull(keyValues, nameof(keyValues));

        //using (CreateTransactionScope())
        return CachedDbSet<T>().Find(keyValues);
    }

    public ValueTask<T?> FindAsync<T>(object[] keyValues, CancellationToken cancellationToken = default) where T : class
    {
        ArgumentNullException.ThrowIfNull(keyValues, nameof(keyValues));

        //using (CreateTransactionScope())
        return CachedDbSet<T>().FindAsync(keyValues, cancellationToken);
    }

    public T? FirstOrDefault<T>(Expression<Func<T, bool>> predicate) where T : class
    {
        ArgumentNullException.ThrowIfNull(predicate, nameof(predicate));

        //using (CreateTransactionScope())
        return AsQueryable<T>().FirstOrDefault(predicate);
    }

    public T? FirstOrDefault<T>() where T : class
    {
        //using (CreateTransactionScope())
        return AsQueryable<T>().FirstOrDefault();
    }

    public Task<T?> FirstOrDefaultAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) where T : class
    {
        ArgumentNullException.ThrowIfNull(predicate, nameof(predicate));

        //using (CreateTransactionScope())
        return AsQueryable<T>().FirstOrDefaultAsync(predicate, cancellationToken);
    }

    public Task<T?> FirstOrDefaultAsync<T>(CancellationToken cancellationToken = default) where T : class
    {
        //using (CreateTransactionScope())
        return AsQueryable<T>().FirstOrDefaultAsync(cancellationToken);
    }

    public T? GetById<T>(object id) where T : class
    {
        return Find<T>(id);
    }

    public ValueTask<T?> GetByIdAsync<T>(object id, CancellationToken cancellationToken = default) where T : class
    {
        return FindAsync<T>(new object[] { id }, cancellationToken);
    }

    public void Insert<T>(T entity) where T : class
    {
        ArgumentNullException.ThrowIfNull(entity, nameof(entity));

        CachedDbSet<T>().Add((T)_funcCreatedAt(entity));
    }

    public void Insert<T>(params T[] entities) where T : class
    {
        this._InternalInsert<T>(entities);
    }

    public void Insert<T>(IEnumerable<T> entities) where T : class
    {
        this._InternalInsert<T>(entities);
    }

    public ValueTask<EntityEntry<T>> InsertAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class
    {
        ArgumentNullException.ThrowIfNull(entity, nameof(entity));

        return CachedDbSet<T>().AddAsync((T)_funcCreatedAt(entity), cancellationToken);
    }

    public Task InsertAsync<T>(T[] entities, CancellationToken cancellationToken = default) where T : class
    {
        return _InternalInsertAsync<T>(entities, cancellationToken);
    }

    public Task InsertAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default) where T : class
    {
        return _InternalInsertAsync<T>(entities, cancellationToken);
    }

    public void Update<T>(params T[] entities) where T : class
    {
        base.UpdateRange(entities);
    }

    public void Update<T>(IEnumerable<T> entities) where T : class
    {
        base.UpdateRange(entities);
    }

    public IQueryable<T> Where<T>(Expression<Func<T, bool>> predicate) where T : class
    {
        ArgumentNullException.ThrowIfNull(predicate, nameof(predicate));

        return AsQueryable<T>().Where(predicate);
    }

    //public Task<List<T>> WhereWithTransactionScopeAsync<T>(Expression<Func<T, bool>> predicate) where T : class
    //{
    //    ArgumentNullException.ThrowIfNull(predicate, nameof(predicate));

    //    using (CreateTransactionScope())
    //        return AsQueryable<T>().Where(predicate).ToListAsync();
    //}

    private void _InternalInsert<T>(IEnumerable<T> entities) where T : class
    {
        ArgumentNullException.ThrowIfNull(entities, nameof(entities));

        CachedDbSet<T>().AddRange(entities.Select(f => (T)_funcCreatedAt(f)));
    }

    private Task _InternalInsertAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default) where T : class
    {
        ArgumentNullException.ThrowIfNull(entities, nameof(entities));

        return CachedDbSet<T>().AddRangeAsync(entities.Select(f => (T)_funcCreatedAt(f)), cancellationToken);
    }

    //private TransactionScope CreateTransactionScope()
    //{
    //    TransactionScopeAsyncFlowOption transactionScopeAsyncFlowOption =
    //        transactionScopeOption == TransactionScopeOption.Suppress ? TransactionScopeAsyncFlowOption.Suppress : TransactionScopeAsyncFlowOption.Suppress;
    //    return new TransactionScope(transactionScopeOption, transactionOptions, transactionScopeAsyncFlowOption);
    //}
}