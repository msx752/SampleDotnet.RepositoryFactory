namespace SampleDotnet.RepositoryFactory;

/// <summary>
/// Base class for repository implementations providing basic CRUD operations and caching mechanisms.
/// Implements the <see cref="IRepository"/> interface.
/// </summary>
internal abstract class RepositoryBase : IRepository, IDisposable
{
    // A function to update the UpdatedAt property for entities implementing IHasDateTimeOffset.
    private static readonly Func<object, object> _funcUpdatedAt = new((entity) =>
    {
        if (entity is IHasDateTimeOffset dt)
            dt.UpdatedAt = DateTimeOffset.Now;
        return entity;
    });

    // A cache to hold DbSets, keyed by entity type name.
    private readonly ConcurrentDictionary<string, IQueryable> _cachedDbSets = new();

    // The DbContext used by the repository.
    private readonly DbContext _context;
    // Indicates whether the object has been disposed.
    private bool disposedValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="RepositoryBase"/> class with the specified DbContext.
    /// </summary>
    /// <param name="context">The DbContext instance.</param>
    protected RepositoryBase(DbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets the <see cref="ChangeTracker"/> associated with the DbContext.
    /// </summary>
    internal ChangeTracker ChangeTracker => _context.ChangeTracker;

    /// <summary>
    /// Gets the <see cref="DatabaseFacade"/> associated with the DbContext.
    /// </summary>
    internal DatabaseFacade Database => _context.Database;

    /// <summary>
    /// Gets the DbContext instance used by the repository.
    /// </summary>
    internal DbContext DbContext => _context;
    /// <summary>
    /// Deletes a specified entity from the DbContext.
    /// </summary>
    /// <param name="entity">The entity to delete.</param>
    public void Delete(object entity)
    {
        ArgumentNullException.ThrowIfNull(entity, nameof(entity));
        _context.Remove(entity);
    }

    /// <summary>
    /// Deletes a range of entities from the DbContext.
    /// </summary>
    /// <param name="entities">The entities to delete.</param>
    public void DeleteRange(params object[] entities)
    {
        ArgumentNullException.ThrowIfNull(entities, nameof(entities));
        _context.RemoveRange(entities);
    }

    /// <summary>
    /// Disposes the repository and releases all resources.
    /// </summary>
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method.
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Updates a specified entity in the DbContext.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    public void Update(object entity)
    {
        ArgumentNullException.ThrowIfNull(entity, nameof(entity));
        _context.Update(_funcUpdatedAt(entity));
    }

    /// <summary>
    /// Updates a range of entities in the DbContext, setting the UpdatedAt property if applicable.
    /// </summary>
    /// <param name="entities">The entities to update.</param>
    public void UpdateRange(params object[] entities)
    {
        // Updates entities and sets their UpdatedAt property if they implement IHasDateTimeOffset.
        ArgumentNullException.ThrowIfNull(entities, nameof(entities));
        _context.UpdateRange(entities.Select(f => _funcUpdatedAt(f)));
    }

    /// <summary>
    /// Gets a cached DbSet for a specific entity type.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <returns>The cached DbSet for the specified entity type.</returns>
    internal DbSet<T> CachedDbSet<T>() where T : class
    {
        // Retrieves or adds a DbSet for the specified type to the cache.
        return (DbSet<T>)_cachedDbSets.GetOrAdd(typeof(T).FullName, DbContext.Set<T>());
    }

    /// <summary>
    /// Disposes the resources used by the repository.
    /// </summary>
    /// <param name="disposing">Indicates whether the method is being called by Dispose() or a finalizer.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // Clear the cache of DbSets.
                _cachedDbSets.Clear();
            }

            disposedValue = true;
        }
    }
}