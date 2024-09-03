namespace SampleDotnet.RepositoryFactory.Interfaces.Repositories;

/// <summary>
/// Interface for asynchronous read operations in a repository, providing methods to retrieve entities asynchronously.
/// </summary>
public interface IReadAsyncRepository
{
    /// <summary>
    /// Asynchronously finds an entity by its composite key values.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="keyValues">An array of key values for the entity.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous operation, with the entity found, or null.</returns>
    ValueTask<T?> FindAsync<T>(object[] keyValues, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Asynchronously retrieves an entity by its primary key.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="id">The primary key of the entity.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous operation, with the entity found, or null.</returns>
    ValueTask<T?> GetByIdAsync<T>(object id, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Asynchronously returns the first entity that matches the specified predicate or null if no such entity is found.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous operation, with the first entity that matches the predicate, or null.</returns>
    Task<T?> FirstOrDefaultAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Asynchronously returns the first entity in the sequence or null if no such entity is found.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous operation, with the first entity in the sequence, or null.</returns>
    Task<T?> FirstOrDefaultAsync<T>(CancellationToken cancellationToken = default) where T : class;
}