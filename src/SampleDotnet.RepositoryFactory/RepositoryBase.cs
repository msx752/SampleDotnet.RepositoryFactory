using Microsoft.EntityFrameworkCore.Infrastructure;

namespace SampleDotnet.RepositoryFactory;

internal abstract class RepositoryBase : IRepository
{
    private static readonly Func<object, object> _funcUpdatedAt = new((entity) =>
    {
        if (entity is IHasDateTimeOffset dt)
            dt.UpdatedAt = DateTimeOffset.Now;
        return entity;
    });

    private readonly DbContext _context;
    private readonly ConcurrentDictionary<string, IQueryable> _cachedDbSets = new();
    private bool disposedValue;

    internal DbContext DbContext => _context;

    public DatabaseFacade Database => _context.Database;

    protected RepositoryBase(DbContext context)
    {
        this._context = context;
    }

    public void Delete(object entity)
    {
        ArgumentNullException.ThrowIfNull(entity, nameof(entity));

        DeleteRange(entity);
    }

    public void DeleteRange(params object[] entities)
    {
        _context.RemoveRange(entities);
    }

    public void Update(object entity)
    {
        ArgumentNullException.ThrowIfNull(entity, nameof(entity));

        UpdateRange(entity);
    }

    public void UpdateRange(params object[] entities)
    {
        _context.UpdateRange(entities.Select(f => _funcUpdatedAt(f)));
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                _cachedDbSets.Clear();
            }

            disposedValue = true;
        }
    }

    internal DbSet<T> CachedDbSet<T>() where T : class
    {
        return (DbSet<T>)_cachedDbSets.GetOrAdd(typeof(T).FullName, DbContext.Set<T>());
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}