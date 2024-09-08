namespace SampleDotnet.RepositoryFactory.Repositories.Implementations;

/// <summary>
/// A generic repository class that provides CRUD operations and additional methods for querying and manipulating entities.
/// Inherits from <see cref="RepositoryBase"/> and implements <see cref="IRepository{TDbContext}"/>.
/// </summary>
/// <typeparam name="TDbContext">The type of the Db.</typeparam>
internal sealed class Repository<TDbContext> : RepositoryBase, IRepository<TDbContext> where TDbContext : DbContext
{
    // The DbContext used by the repository.

    /// <summary>
    /// Initializes a new instance of the <see cref="Repository{TDbContext}"/> class with the specified Db.
    /// </summary>
    /// <param name="dbContext">The DbContext instance.</param>
    internal Repository(TDbContext dbContext)
        : base(dbContext)
    {
    }

    /// <summary>
    /// Returns an <see cref="IQueryable{T}"/> of the specified entity type.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <returns>An IQueryable of the specified entity type.</returns>
    public IQueryable<T> AsNoTracking<T>() where T : class
    {
        return Db.Set<T>().AsNoTracking();
    }

    /// <summary>
    /// Returns an <see cref="IQueryable{T}"/> of the specified entity type.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <returns>An IQueryable of the specified entity type.</returns>
    public IQueryable<T> AsQueryable<T>() where T : class
    {
        return Db.Set<T>();
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
        return Db.Set<T>().Find(keyValues);
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
        return Db.Set<T>().FindAsync(keyValues, cancellationToken);
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
        return Db.Set<T>().FirstOrDefault(predicate);
    }

    /// <summary>
    /// Returns the first entity in the sequence, or null if no such entity is found.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <returns>The first entity in the sequence, or null.</returns>
    public T? FirstOrDefault<T>() where T : class
    {
        return Db.Set<T>().FirstOrDefault();
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
        return Db.Set<T>().FirstOrDefaultAsync(predicate, cancellationToken);
    }

    /// <summary>
    /// Asynchronously returns the first entity in the sequence, or null if no such entity is found.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous operation, with the first entity in the sequence, or null.</returns>
    public Task<T?> FirstOrDefaultAsync<T>(CancellationToken cancellationToken = default) where T : class
    {
        return Db.Set<T>().FirstOrDefaultAsync(cancellationToken);
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
        return Db.Set<TEntity>().Where(predicate);
    }
}