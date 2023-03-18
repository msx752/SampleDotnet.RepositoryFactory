namespace SampleDotnet.RepositoryFactory;

public class Repository<TDbContext>
    : IRepository<TDbContext>, IDisposable
    where TDbContext : DbContext
{
    private readonly TDbContext _context;
    private bool disposedValue;

    public Repository(TDbContext dbContext)
    {
        _context = dbContext;
    }

    public DbContext CurrentDbContext => _context;

    public IQueryable<T> AsQueryable<T>() where T : class
    {
        return CurrentDbContext.Set<T>().AsQueryable<T>();
    }

    public void Delete<T>(T entity) where T : class
    {
        var entry = CurrentDbContext.Entry(entity);
        entry.State = EntityState.Deleted;
    }

    public void Delete<T>(params T[] entities) where T : class
    {
        foreach (var item in entities)
            Delete(item);
    }

    public void Delete<T>(IEnumerable<T> entities) where T : class
    {
        foreach (var item in entities)
            Delete(item);
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public T? Find<T>(params object[] keyValues) where T : class
    {
        return CurrentDbContext.Set<T>().Find(keyValues);
    }

    public ValueTask<T?> FindAsync<T>(object[] keyValues, CancellationToken cancellationToken = default) where T : class
    {
        return CurrentDbContext.Set<T>().FindAsync(keyValues, cancellationToken);
    }

    public T? FirstOrDefault<T>(Expression<Func<T, bool>> predicate) where T : class
    {
        IQueryable<T> query = AsQueryable<T>();
        return query.FirstOrDefault(predicate);
    }

    public Task<T?> FirstOrDefaultAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) where T : class
    {
        IQueryable<T> query = AsQueryable<T>();
        return query.FirstOrDefaultAsync(predicate, cancellationToken);
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
        if (entity is IHasDateTimeOffset dt)
            dt.CreatedAt = DateTimeOffset.Now;

        CurrentDbContext.Set<T>().Add(entity);
    }

    public void Insert<T>(params T[] entities) where T : class
    {
        CurrentDbContext.Set<T>().AddRange(entities.Select(f =>
        {
            if (f is IHasDateTimeOffset dt)
                dt.CreatedAt = DateTimeOffset.Now;

            return f;
        }));
    }

    public void Insert<T>(IEnumerable<T> entities) where T : class
    {
        CurrentDbContext.Set<T>().AddRange(entities.Select(f =>
        {
            if (f is IHasDateTimeOffset dt)
                dt.CreatedAt = DateTimeOffset.Now;

            return f;
        }));
    }

    public ValueTask<EntityEntry<T>> InsertAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class
    {
        if (entity is IHasDateTimeOffset dt)
            dt.CreatedAt = DateTimeOffset.Now;

        return CurrentDbContext.Set<T>().AddAsync(entity, cancellationToken);
    }

    public Task InsertAsync<T>(T[] entities, CancellationToken cancellationToken = default) where T : class
    {
        return CurrentDbContext.Set<T>().AddRangeAsync(entities.Select(f =>
        {
            if (f is IHasDateTimeOffset dt)
                dt.CreatedAt = DateTimeOffset.Now;

            return f;
        }), cancellationToken);
    }

    public Task InsertAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default) where T : class
    {
        return CurrentDbContext.Set<T>().AddRangeAsync(entities.Select(f =>
        {
            if (f is IHasDateTimeOffset dt)
                dt.CreatedAt = DateTimeOffset.Now;

            return f;
        }), cancellationToken);
    }

    public int SaveChanges()
    {
        var result = CurrentDbContext.SaveChanges();
        return result;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var result = CurrentDbContext.SaveChangesAsync(cancellationToken);
        return result;
    }

    public void Update<T>(T entity) where T : class
    {
        var entry = CurrentDbContext.Entry(entity);
        entry.State = EntityState.Modified;

        if (entry.Entity is IHasDateTimeOffset dt)
            dt.UpdatedAt = DateTimeOffset.Now;
    }

    public void Update<T>(params T[] entities) where T : class
    {
        for (int i = 0; i < entities.Length; i++)
            Update<T>(entities[i]);
    }

    public void Update<T>(IEnumerable<T> entities) where T : class
    {
        for (int i = 0; i < entities.Count(); i++)
            Update<T>(entities.ElementAt(i));
    }

    public IQueryable<T> Where<T>(Expression<Func<T, bool>> predicate) where T : class
    {
        return AsQueryable<T>().Where(predicate);
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