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

    public void Dispose()
    {
        cachedDbSets.Clear();
        GC.SuppressFinalize(this);
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
}