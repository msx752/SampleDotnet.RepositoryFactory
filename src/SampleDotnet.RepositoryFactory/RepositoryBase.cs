using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace SampleDotnet.RepositoryFactory;

internal abstract class RepositoryBase : IRepository
{
    private static readonly Func<object, object> funcUpdatedAt = new((entity) =>
    {
        if (entity is IHasDateTimeOffset dt)
            dt.UpdatedAt = DateTimeOffset.Now;
        return entity;
    });

    private readonly DbContext _context;
    internal readonly ConcurrentDictionary<string, IQueryable> cachedDbSets = new();
    private bool disposedValue;

    protected RepositoryBase(DbContext context)
    {
        this._context = context;
    }

    public DbContext CurrentDbContext => _context;

    public virtual void Delete(object entity)
    {
        ArgumentNullException.ThrowIfNull(entity, nameof(entity));

        DeleteRange(entity);
    }

    public virtual void DeleteRange(params object[] entities)
    {
        _context.RemoveRange(entities);
    }

    public virtual void Update(object entity)
    {
        ArgumentNullException.ThrowIfNull(entity, nameof(entity));

        UpdateRange(entity);
    }

    public virtual void UpdateRange(params object[] entities)
    {
        _context.UpdateRange(entities.Select(f => funcUpdatedAt(f)));
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                cachedDbSets.Clear();
            }

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}