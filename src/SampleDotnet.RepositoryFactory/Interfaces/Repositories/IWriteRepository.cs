namespace SampleDotnet.RepositoryFactory.Interfaces.Repositories;

/// <summary>
/// Interface for synchronous write operations in a repository, providing methods to insert, update, and delete entities.
/// </summary>
public interface IWriteRepository
{
    /// <summary>
    /// Inserts a new entity into the repository.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entity">The entity to insert.</param>
    void Insert<T>(T entity) where T : class;

    void AddRange<T>(IEnumerable<T> entities) where T : class;

    void Add<T>(T entity) where T : class;

    /// <summary>
    /// Inserts a range of entities into the repository.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entities">The entities to insert.</param>
    void Insert<T>(params T[] entities) where T : class;

    /// <summary>
    /// Inserts a collection of entities into the repository.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entities">The collection of entities to insert.</param>
    void Insert<T>(IEnumerable<T> entities) where T : class;

    /// <summary>
    /// Updates the specified entities in the repository.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entities">The entities to update.</param>
    void Update<T>(params T[] entities) where T : class;

    /// <summary>
    /// Updates a collection of entities in the repository.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entities">The collection of entities to update.</param>
    void Update<T>(IEnumerable<T> entities) where T : class;

    /// <summary>
    /// Deletes the specified entity from the repository.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entity">The entity to delete.</param>
    void Delete<T>(T entity) where T : class;

    /// <summary>
    /// Deletes the specified entities from the repository.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entities">The entities to delete.</param>
    void Delete<T>(params T[] entities) where T : class;

    /// <summary>
    /// Deletes a collection of entities from the repository.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entities">The collection of entities to delete.</param>
    void Delete<T>(IEnumerable<T> entities) where T : class;
}