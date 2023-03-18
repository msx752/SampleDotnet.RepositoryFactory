namespace SampleDotnet.RepositoryFactory;

public abstract class RepositoryBase : IRepository
{
    private readonly DbContext _context;

    private bool disposedValue;

    public RepositoryBase(DbContext context)
    {
        this._context = context;
    }

    public DbContext CurrentDbContext => _context;

    public virtual void Delete(object entity)
    {
        ArgumentNullException.ThrowIfNull(entity, nameof(entity));

        var entry = CurrentDbContext.Entry(entity);
        entry.State = EntityState.Deleted;
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public virtual int SaveChanges()
    {
        return CurrentDbContext.SaveChanges();
    }

    public virtual Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return CurrentDbContext.SaveChangesAsync(cancellationToken);
    }

    public virtual void Update(object entity)
    {
        ArgumentNullException.ThrowIfNull(entity, nameof(entity));

        var entry = CurrentDbContext.Entry(entity);
        entry.State = EntityState.Modified;

        if (entry.Entity is IHasDateTimeOffset dt)
            dt.UpdatedAt = DateTimeOffset.Now;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                try
                {
                    CurrentDbContext.Dispose();
                }
                catch
                {
                }
            }

            disposedValue = true;
        }
    }
}

public class Repository<TDbContext> : RepositoryBase
    , IRepository<TDbContext>
    where TDbContext : DbContext
{
    public Repository(TDbContext dbContext)
        : base(dbContext)
    {
    }

    public IQueryable<T> AsQueryable<T>() where T : class
    {
        return CurrentDbContext.Set<T>().AsQueryable<T>();
    }

    public void Delete<T>(T entity) where T : class
    {
        ArgumentNullException.ThrowIfNull(entity, nameof(entity));

        base.Delete(entity);
    }

    public void Delete<T>(params T[] entities) where T : class
    {
        ArgumentNullException.ThrowIfNull(entities, nameof(entities));

        foreach (var item in entities)
            Delete(item);
    }

    public void Delete<T>(IEnumerable<T> entities) where T : class
    {
        ArgumentNullException.ThrowIfNull(entities, nameof(entities));

        foreach (var item in entities)
            Delete(item);
    }

    public T? Find<T>(params object[] keyValues) where T : class
    {
        ArgumentNullException.ThrowIfNull(keyValues, nameof(keyValues));

        return CurrentDbContext.Set<T>().Find(keyValues);
    }

    public ValueTask<T?> FindAsync<T>(object[] keyValues, CancellationToken cancellationToken = default) where T : class
    {
        ArgumentNullException.ThrowIfNull(keyValues, nameof(keyValues));

        return CurrentDbContext.Set<T>().FindAsync(keyValues, cancellationToken);
    }

    public T? FirstOrDefault<T>(Expression<Func<T, bool>> predicate) where T : class
    {
        ArgumentNullException.ThrowIfNull(predicate, nameof(predicate));

        IQueryable<T> query = AsQueryable<T>();
        return query.FirstOrDefault(predicate);
    }

    public Task<T?> FirstOrDefaultAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) where T : class
    {
        ArgumentNullException.ThrowIfNull(predicate, nameof(predicate));

        IQueryable<T> query = AsQueryable<T>();
        return query.FirstOrDefaultAsync(predicate, cancellationToken);
    }

    public T? GetById<T>(object id) where T : class
    {
        ArgumentNullException.ThrowIfNull(id, nameof(id));

        return Find<T>(id);
    }

    public ValueTask<T?> GetByIdAsync<T>(object id, CancellationToken cancellationToken = default) where T : class
    {
        ArgumentNullException.ThrowIfNull(id, nameof(id));

        return FindAsync<T>(new object[] { id }, cancellationToken);
    }

    public void Insert<T>(T entity) where T : class
    {
        ArgumentNullException.ThrowIfNull(entity, nameof(entity));

        if (entity is IHasDateTimeOffset dt)
            dt.CreatedAt = DateTimeOffset.Now;

        CurrentDbContext.Set<T>().Add(entity);
    }

    public void Insert<T>(params T[] entities) where T : class
    {
        ArgumentNullException.ThrowIfNull(entities, nameof(entities));

        CurrentDbContext.Set<T>().AddRange(entities.Select(f =>
        {
            if (f is IHasDateTimeOffset dt)
                dt.CreatedAt = DateTimeOffset.Now;

            return f;
        }));
    }

    public void Insert<T>(IEnumerable<T> entities) where T : class
    {
        ArgumentNullException.ThrowIfNull(entities, nameof(entities));

        CurrentDbContext.Set<T>().AddRange(entities.Select(f =>
        {
            if (f is IHasDateTimeOffset dt)
                dt.CreatedAt = DateTimeOffset.Now;

            return f;
        }));
    }

    public ValueTask<EntityEntry<T>> InsertAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class
    {
        ArgumentNullException.ThrowIfNull(entity, nameof(entity));

        if (entity is IHasDateTimeOffset dt)
            dt.CreatedAt = DateTimeOffset.Now;

        return CurrentDbContext.Set<T>().AddAsync(entity, cancellationToken);
    }

    public Task InsertAsync<T>(T[] entities, CancellationToken cancellationToken = default) where T : class
    {
        ArgumentNullException.ThrowIfNull(entities, nameof(entities));

        return CurrentDbContext.Set<T>().AddRangeAsync(entities.Select(f =>
        {
            if (f is IHasDateTimeOffset dt)
                dt.CreatedAt = DateTimeOffset.Now;

            return f;
        }), cancellationToken);
    }

    public Task InsertAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default) where T : class
    {
        ArgumentNullException.ThrowIfNull(entities, nameof(entities));

        return CurrentDbContext.Set<T>().AddRangeAsync(entities.Select(f =>
        {
            if (f is IHasDateTimeOffset dt)
                dt.CreatedAt = DateTimeOffset.Now;

            return f;
        }), cancellationToken);
    }

    public void Update<T>(T entity) where T : class
    {
        base.Update(entity);
    }

    public void Update<T>(params T[] entities) where T : class
    {
        ArgumentNullException.ThrowIfNull(entities, nameof(entities));

        for (int i = 0; i < entities.Length; i++)
            Update<T>(entities[i]);
    }

    public void Update<T>(IEnumerable<T> entities) where T : class
    {
        ArgumentNullException.ThrowIfNull(entities, nameof(entities));

        for (int i = 0; i < entities.Count(); i++)
            Update<T>(entities.ElementAt(i));
    }

    public IQueryable<T> Where<T>(Expression<Func<T, bool>> predicate) where T : class
    {
        ArgumentNullException.ThrowIfNull(predicate, nameof(predicate));

        return AsQueryable<T>().Where(predicate);
    }
}