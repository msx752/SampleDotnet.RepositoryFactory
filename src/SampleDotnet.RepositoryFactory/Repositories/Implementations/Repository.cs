namespace SampleDotnet.RepositoryFactory.Repositories.Implementations;

/// <summary>
/// A generic repository class that provides CRUD operations and additional methods for querying and manipulating entities.
/// Inherits from <see cref="RepositoryBase"/> and implements <see cref="IRepository{TDbContext}"/>.
/// </summary>
/// <typeparam name="TDbContext">The type of the _context.</typeparam>
internal sealed class Repository<TDbContext> : IRepository<TDbContext> where TDbContext : DbContext
{
    // The DbContext used by the repository.
    private readonly DbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="Repository{TDbContext}"/> class with the specified _context.
    /// </summary>
    /// <param name="dbContext">The DbContext instance.</param>
    internal Repository(TDbContext dbContext)
    {
        _context = dbContext;
    }

    public ChangeTracker ChangeTracker => _context.ChangeTracker;

    public DatabaseFacade Database => _context.Database;

    public DbContext DbContext => _context;

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

    /// <summary>
    /// Returns an <see cref="IQueryable{T}"/> of the specified entity type.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <returns>An IQueryable of the specified entity type.</returns>
    public IQueryable<T> AsNoTracking<T>() where T : class
    {
        return _context.Set<T>().AsNoTracking();
    }

    /// <summary>
    /// Returns an <see cref="IQueryable{T}"/> of the specified entity type.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <returns>An IQueryable of the specified entity type.</returns>
    public IQueryable<T> AsQueryable<T>() where T : class
    {
        return _context.Set<T>();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Finds an entity with the given primary key values.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="keyValues">The values of the primary key for the entity to be found.</param>
    /// <returns>The entity found, or null.</returns>
    public T? Find<T>(params object[] keyValues) where T : class
    {
        ArgumentNullException.ThrowIfNull(keyValues, nameof(keyValues));
        return _context.Set<T>().Find(keyValues);
    }

    /// <summary>
    /// Asynchronously finds an entity with the given primary key values.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="keyValues">The values of the primary key for the entity to be found.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous operation, with the entity found, or null.</returns>
    public ValueTask<T?> FindAsync<T>(object[] keyValues, CancellationToken cancellationToken = default) where T : class
    {
        ArgumentNullException.ThrowIfNull(keyValues, nameof(keyValues));
        return _context.Set<T>().FindAsync(keyValues, cancellationToken);
    }

    /// <summary>
    /// Returns the first entity that matches the specified predicate, or null if no such entity is found.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <returns>The first entity that matches the specified predicate, or null.</returns>
    public T? FirstOrDefault<T>(Expression<Func<T, bool>> predicate) where T : class
    {
        ArgumentNullException.ThrowIfNull(predicate, nameof(predicate));
        return _context.Set<T>().FirstOrDefault(predicate);
    }

    /// <summary>
    /// Returns the first entity in the sequence, or null if no such entity is found.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <returns>The first entity in the sequence, or null.</returns>
    public T? FirstOrDefault<T>() where T : class
    {
        return _context.Set<T>().FirstOrDefault();
    }

    /// <summary>
    /// Asynchronously returns the first entity that matches the specified predicate, or null if no such entity is found.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous operation, with the first entity that matches the specified predicate, or null.</returns>
    public Task<T?> FirstOrDefaultAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) where T : class
    {
        ArgumentNullException.ThrowIfNull(predicate, nameof(predicate));
        return _context.Set<T>().FirstOrDefaultAsync(predicate, cancellationToken);
    }

    /// <summary>
    /// Asynchronously returns the first entity in the sequence, or null if no such entity is found.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous operation, with the first entity in the sequence, or null.</returns>
    public Task<T?> FirstOrDefaultAsync<T>(CancellationToken cancellationToken = default) where T : class
    {
        return _context.Set<T>().FirstOrDefaultAsync(cancellationToken);
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

    /// <summary>
    /// Returns an <see cref="IQueryable{T}"/> that contains elements from the input sequence that satisfy the specified predicate.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <returns>An IQueryable that contains elements from the input sequence that satisfy the condition specified by predicate.</returns>
    public IQueryable<TEntity> Where<TEntity>(Expression<Func<TEntity, bool>> predicate) where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(predicate, nameof(predicate));
        return _context.Set<TEntity>().Where(predicate);
    }
}