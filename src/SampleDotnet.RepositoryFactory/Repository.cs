namespace SampleDotnet.RepositoryFactory;

/// <summary>
/// A generic repository class that provides CRUD operations and additional methods for querying and manipulating entities.
/// Inherits from <see cref="RepositoryBase"/> and implements <see cref="IRepository{TDbContext}"/>.
/// </summary>
/// <typeparam name="TDbContext">The type of the DbContext.</typeparam>
internal class Repository<TDbContext> : RepositoryBase, IRepository<TDbContext> where TDbContext : DbContext
{
    // A function to set the CreatedAt property for entities implementing IHasDateTimeOffset.
    private static readonly Func<object, object> _funcCreatedAt = new((entity) =>
    {
        if (entity is IHasDateTimeOffset dt)
            dt.CreatedAt = DateTimeOffset.Now;
        return entity;
    });

    /// <summary>
    /// Initializes a new instance of the <see cref="Repository{TDbContext}"/> class with the specified DbContext.
    /// </summary>
    /// <param name="dbContext">The DbContext instance.</param>
    public Repository(TDbContext dbContext)
        : base(dbContext)
    {
    }

    /// <summary>
    /// Returns an <see cref="IQueryable{T}"/> of the specified entity type.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <returns>An IQueryable of the specified entity type.</returns>
    public IQueryable<T> AsQueryable<T>() where T : class
    {
        return CachedDbSet<T>();
    }

    /// <summary>
    /// Returns an <see cref="IQueryable{T}"/> of the specified entity type.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <returns>An IQueryable of the specified entity type.</returns>
    public IQueryable<T> AsQueryableWithNoTracking<T>() where T : class
    {
        return CachedDbSet<T>().AsNoTracking();
    }

    /// <summary>
    /// Deletes the specified entity from the DbContext.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entity">The entity to delete.</param>
    public void Delete<T>(T entity) where T : class
    {
        ArgumentNullException.ThrowIfNull(entity, nameof(entity));

        base.Delete(entity);
    }

    /// <summary>
    /// Deletes the specified entities from the DbContext.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entities">The entities to delete.</param>
    public void Delete<T>(params T[] entities) where T : class
    {
        ArgumentNullException.ThrowIfNull(entities, nameof(entities));

        base.DeleteRange(entities);
    }

    /// <summary>
    /// Deletes the specified entities from the DbContext.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entities">The entities to delete.</param>
    public void Delete<T>(IEnumerable<T> entities) where T : class
    {
        ArgumentNullException.ThrowIfNull(entities, nameof(entities));

        CachedDbSet<T>().RemoveRange(entities);
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

        return CachedDbSet<T>().Find(keyValues);
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

        return CachedDbSet<T>().FindAsync(keyValues, cancellationToken);
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

        return AsQueryable<T>().FirstOrDefault(predicate);
    }

    /// <summary>
    /// Returns the first entity in the sequence, or null if no such entity is found.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <returns>The first entity in the sequence, or null.</returns>
    public T? FirstOrDefault<T>() where T : class
    {
        return AsQueryable<T>().FirstOrDefault();
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

        return AsQueryable<T>().FirstOrDefaultAsync(predicate, cancellationToken);
    }

    /// <summary>
    /// Asynchronously returns the first entity in the sequence, or null if no such entity is found.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous operation, with the first entity in the sequence, or null.</returns>
    public Task<T?> FirstOrDefaultAsync<T>(CancellationToken cancellationToken = default) where T : class
    {
        return AsQueryable<T>().FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Finds an entity by its ID.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="id">The ID of the entity.</param>
    /// <returns>The entity found, or null.</returns>
    public T? GetById<T>(object id) where T : class
    {
        return Find<T>(id);
    }

    /// <summary>
    /// Asynchronously finds an entity by its ID.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="id">The ID of the entity.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous operation, with the entity found, or null.</returns>
    public ValueTask<T?> GetByIdAsync<T>(object id, CancellationToken cancellationToken = default) where T : class
    {
        return FindAsync<T>(new object[] { id }, cancellationToken);
    }

    /// <summary>
    /// Inserts a new entity into the DbContext.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entity">The entity to insert.</param>
    public void Insert<T>(T entity) where T : class
    {
        ArgumentNullException.ThrowIfNull(entity, nameof(entity));

        CachedDbSet<T>().Add((T)_funcCreatedAt(entity));
    }

    /// <summary>
    /// Inserts a range of entities into the DbContext.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entities">The entities to insert.</param>
    public void Insert<T>(params T[] entities) where T : class
    {
        ArgumentNullException.ThrowIfNull(entities, nameof(entities));

        CachedDbSet<T>().AddRange(entities.Select(f => (T)_funcCreatedAt(f)));
    }

    /// <summary>
    /// Inserts a range of entities into the DbContext.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entities">The entities to insert.</param>
    public void Insert<T>(IEnumerable<T> entities) where T : class
    {
        ArgumentNullException.ThrowIfNull(entities, nameof(entities));

        CachedDbSet<T>().AddRange(entities.Select(f => (T)_funcCreatedAt(f)));
    }

    /// <summary>
    /// Asynchronously inserts a new entity into the DbContext.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entity">The entity to insert.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public ValueTask<EntityEntry<T>> InsertAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class
    {
        ArgumentNullException.ThrowIfNull(entity, nameof(entity));

        return CachedDbSet<T>().AddAsync((T)_funcCreatedAt(entity), cancellationToken);
    }

    /// <summary>
    /// Asynchronously inserts a range of entities into the DbContext.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entities">The entities to insert.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task InsertAsync<T>(T[] entities, CancellationToken cancellationToken = default) where T : class
    {
        ArgumentNullException.ThrowIfNull(entities, nameof(entities));

        return CachedDbSet<T>().AddRangeAsync(entities.Select(f => (T)_funcCreatedAt(f)), cancellationToken);
    }

    /// <summary>
    /// Asynchronously inserts a range of entities into the DbContext.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entities">The entities to insert.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task InsertAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default) where T : class
    {
        ArgumentNullException.ThrowIfNull(entities, nameof(entities));

        return CachedDbSet<T>().AddRangeAsync(entities.Select(f => (T)_funcCreatedAt(f)), cancellationToken);
    }

    /// <summary>
    /// Updates a range of entities in the DbContext.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entities">The entities to update.</param>
    public void Update<T>(params T[] entities) where T : class
    {
        base.UpdateRange(entities);
    }

    /// <summary>
    /// Updates a range of entities in the DbContext.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entities">The entities to update.</param>
    public void Update<T>(IEnumerable<T> entities) where T : class
    {
        base.UpdateRange(entities);
    }

    /// <summary>
    /// Returns an <see cref="IQueryable{T}"/> that contains elements from the input sequence that satisfy the specified predicate.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <returns>An IQueryable that contains elements from the input sequence that satisfy the condition specified by predicate.</returns>
    public IQueryable<T> Where<T>(Expression<Func<T, bool>> predicate) where T : class
    {
        ArgumentNullException.ThrowIfNull(predicate, nameof(predicate));

        return AsQueryable<T>().Where(predicate);
    }
}