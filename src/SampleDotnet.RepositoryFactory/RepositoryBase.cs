using System.Collections.Concurrent;

namespace SampleDotnet.RepositoryFactory;

internal abstract class RepositoryBase : IRepository
{
    private static readonly Func<object, object> _funcUpdatedAt = new((entity) =>
    {
        if (entity is IHasDateTimeOffset dt)
            dt.UpdatedAt = DateTimeOffset.Now;
        return entity;
    });

    private DbContext context;
    internal readonly ConcurrentDictionary<string, IQueryable> _cachedDbSets = new();

    protected RepositoryBase(DbContext context)
    {
        this.context = context;
    }

    public DbContext DbContext => context;

    public virtual void Delete(object entity)
    {
        ArgumentNullException.ThrowIfNull(entity, nameof(entity));

        DeleteRange(entity);
    }

    public virtual void DeleteRange(params object[] entities)
    {
        context.RemoveRange(entities);
    }

    public void Dispose()
    {
        _cachedDbSets.Clear();
        GC.SuppressFinalize(this);
    }

    public virtual void Update(object entity)
    {
        ArgumentNullException.ThrowIfNull(entity, nameof(entity));

        UpdateRange(entity);
    }

    public virtual void UpdateRange(params object[] entities)
    {
        context.UpdateRange(entities.Select(f => _funcUpdatedAt(f)));
    }

    public abstract DbContext CreateDbContext();

    public DbContext RefreshDbContext()
    {
        try { context.Dispose(); } catch { }

        context = CreateDbContext();

        return context;
    }
}