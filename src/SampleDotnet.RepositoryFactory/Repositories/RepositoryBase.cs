namespace SampleDotnet.RepositoryFactory.Repositories;

internal abstract class RepositoryBase : IRepository
{
    internal readonly DbContext _context;

    public RepositoryBase(DbContext dbContext)
    {
        _context = dbContext;
    }

    public ChangeTracker ChangeTracker => _context.ChangeTracker;

    public DatabaseFacade Database => _context.Database;

    public DbContext Db => _context;

    public EntityEntry Add(object entity)
    {
        ArgumentNullException.ThrowIfNull(entity, nameof(entity));
        return _context.Add(entity);
    }

    public ValueTask<EntityEntry> AddAsync(object entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity, nameof(entity));
        return _context.AddAsync(entity, cancellationToken);
    }

    public void AddRange(params object[] entities)
    {
        ArgumentNullException.ThrowIfNull(entities, nameof(entities));
        _context.AddRange(entities);
    }

    public Task AddRangeAsync(IEnumerable<object> entities, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entities, nameof(entities));
        return _context.AddRangeAsync(entities, cancellationToken);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Deletes the specified entity from the _context.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entity">The entity to delete.</param>
    public EntityEntry Remove(object entity)
    {
        ArgumentNullException.ThrowIfNull(entity, nameof(entity));
        return _context.Remove(entity);
    }

    /// <summary>
    /// Deletes the specified entities from the _context.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entities">The entities to delete.</param>
    public void RemoveRange(params object[] entities)
    {
        ArgumentNullException.ThrowIfNull(entities, nameof(entities));
        _context.RemoveRange(entities);
    }

    public DbSet<TEntity> Set<TEntity>() where TEntity : class
    {
        return _context.Set<TEntity>();
    }

    /// <summary>
    /// Updates a range of entities in the _context.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entities">The entities to update.</param>
    public EntityEntry Update(object entity)
    {
        ArgumentNullException.ThrowIfNull(entity, nameof(entity));
        return _context.Update(entity);
    }

    public void UpdateRange(params object[] entities)
    {
        ArgumentNullException.ThrowIfNull(entities, nameof(entities));
        _context.UpdateRange(entities);
    }
}