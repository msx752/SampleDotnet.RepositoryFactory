namespace SampleDotnet.RepositoryFactory.Interfaces;

/// <summary>
/// Represents a generic repository interface that provides basic CRUD operations.
/// </summary>
public interface IRepository : IDisposable
{
    /// <summary>
    /// Deletes a specified entity from the DbContext.
    /// </summary>
    /// <param name="entity">The entity to delete.</param>
    void Delete(object entity);
    /// <summary>
    /// Updates a specified entity in the DbContext.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    void Update(object entity);

    /// <summary>
    /// Gets the <see cref="ChangeTracker"/> associated with the DbContext.
    /// </summary>
    ChangeTracker ChangeTracker { get; }

    /// <summary>
    /// Gets the <see cref="DatabaseFacade"/> instance associated with the DbContext.
    /// </summary>
    DatabaseFacade Database { get; }
    /// <summary>
    /// Deletes a range of entities from the DbContext.
    /// </summary>
    /// <param name="entities">The entities to delete.</param>
    void DeleteRange(params object[] entities);

    /// <summary>
    /// Updates a range of entities in the DbContext.
    /// </summary>
    /// <param name="entities">The entities to update.</param>
    void UpdateRange(params object[] entities);
}

/// <summary>
/// Represents a generic repository interface for a specific <see cref="DbContext"/> type, providing additional functionality.
/// </summary>
/// <typeparam name="TDbContext">The type of the DbContext.</typeparam>
public interface IRepository<TDbContext> : IRepository where TDbContext : DbContext
{
    /// <summary>
    /// Returns an <see cref="IQueryable{T}"/> of the specified entity type.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <returns>An IQueryable of the specified entity type.</returns>
    IQueryable<T> AsQueryable<T>() where T : class;

    /// <summary>
    /// Deletes the specified entity from the DbContext.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entity">The entity to delete.</param>
    void Delete<T>(T entity) where T : class;

    /// <summary>
    /// Deletes the specified entities from the DbContext.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entities">The entities to delete.</param>
    void Delete<T>(params T[] entities) where T : class;

    /// <summary>
    /// Deletes the specified entities from the DbContext.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entities">The entities to delete.</param>
    void Delete<T>(IEnumerable<T> entities) where T : class;

    /// <summary>
    /// Finds an entity with the given primary key values.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="keyValues">The values of the primary key for the entity to be found.</param>
    /// <returns>The entity found, or null.</returns>
    T? Find<T>(params object[] keyValues) where T : class;

    /// <summary>
    /// Asynchronously finds an entity with the given primary key values.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="keyValues">The values of the primary key for the entity to be found.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous operation, with the entity found, or null.</returns>
    ValueTask<T?> FindAsync<T>(object[] keyValues, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Returns the first entity in the sequence, or null if no such entity is found.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <returns>The first entity in the sequence, or null.</returns>
    T? FirstOrDefault<T>() where T : class;

    /// <summary>
    /// Returns the first entity that matches the specified predicate, or null if no such entity is found.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <returns>The first entity that matches the specified predicate, or null.</returns>
    T? FirstOrDefault<T>(Expression<Func<T, bool>> predicate) where T : class;

    /// <summary>
    /// Asynchronously returns the first entity in the sequence, or null if no such entity is found.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous operation, with the first entity in the sequence, or null.</returns>
    Task<T?> FirstOrDefaultAsync<T>(CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Asynchronously returns the first entity that matches the specified predicate, or null if no such entity is found.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous operation, with the first entity that matches the specified predicate, or null.</returns>
    Task<T?> FirstOrDefaultAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Finds an entity by its ID.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="id">The ID of the entity.</param>
    /// <returns>The entity found, or null.</returns>
    T? GetById<T>(object id) where T : class;

    /// <summary>
    /// Asynchronously finds an entity by its ID.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="id">The ID of the entity.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous operation, with the entity found, or null.</returns>
    ValueTask<T?> GetByIdAsync<T>(object id, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Inserts a new entity into the DbContext.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entity">The entity to insert.</param>
    void Insert<T>(T entity) where T : class;

    /// <summary>
    /// Inserts a range of entities into the DbContext.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entities">The entities to insert.</param>
    void Insert<T>(params T[] entities) where T : class;

    /// <summary>
    /// Inserts a range of entities into the DbContext.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entities">The entities to insert.</param>
    void Insert<T>(IEnumerable<T> entities) where T : class;

    /// <summary>
    /// Asynchronously inserts a new entity into the DbContext.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entity">The entity to insert.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    ValueTask<EntityEntry<T>> InsertAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Asynchronously inserts a range of entities into the DbContext.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entities">The entities to insert.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task InsertAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Asynchronously inserts a range of entities into the DbContext.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entities">The entities to insert.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task InsertAsync<T>(T[] entities, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Updates a range of entities in the DbContext.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entities">The entities to update.</param>
    void Update<T>(params T[] entities) where T : class;

    /// <summary>
    /// Updates a range of entities in the DbContext.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entities">The entities to update.</param>
    void Update<T>(IEnumerable<T> entities) where T : class;

    /// <summary>
    /// Returns an <see cref="IQueryable{T}"/> that contains elements from the input sequence that satisfy the specified predicate.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <returns>An IQueryable that contains elements from the input sequence that satisfy the condition specified by predicate.</returns>
    IQueryable<T> Where<T>(Expression<Func<T, bool>> predicate) where T : class;
}