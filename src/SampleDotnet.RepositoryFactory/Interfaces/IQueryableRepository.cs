namespace SampleDotnet.RepositoryFactory.Interfaces;

/// <summary>
/// Interface for querying operations in a repository, providing methods to query entities with or without tracking.
/// </summary>
public interface IQueryableRepository
{
    /// <summary>
    /// Returns an IQueryable of the specified entity type.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <returns>An IQueryable of the specified entity type.</returns>
    IQueryable<T> AsQueryable<T>() where T : class;

    /// <summary>
    /// Returns an IQueryable of the specified entity type without tracking changes.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <returns>An IQueryable of the specified entity type with no tracking enabled.</returns>
    IQueryable<T> AsQueryableWithNoTracking<T>() where T : class;

    /// <summary>
    /// Returns an IQueryable that contains elements from the input sequence that satisfy the specified predicate.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <returns>An IQueryable that contains elements from the input sequence that satisfy the condition specified by predicate.</returns>
    IQueryable<T> Where<T>(Expression<Func<T, bool>> predicate) where T : class;
}
