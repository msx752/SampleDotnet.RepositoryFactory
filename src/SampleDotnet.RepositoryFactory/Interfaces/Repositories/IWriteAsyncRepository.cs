namespace SampleDotnet.RepositoryFactory.Interfaces.Repositories;

/// <summary>
/// Interface for asynchronous write operations in a repository, providing methods to insert entities asynchronously.
/// </summary>
public interface IWriteAsyncRepository
{
    /// <summary>
    /// Asynchronously inserts a new entity into the repository.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entity">The entity to insert.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    ValueTask<EntityEntry<T>> InsertAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Asynchronously inserts a range of entities into the repository.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entities">The entities to insert.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task InsertAsync<T>(T[] entities, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Asynchronously inserts a collection of entities into the repository.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entities">The collection of entities to insert.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task InsertAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default) where T : class;
}