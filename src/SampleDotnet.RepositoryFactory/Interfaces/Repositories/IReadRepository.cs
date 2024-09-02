namespace SampleDotnet.RepositoryFactory.Interfaces.Repositories;

/// <summary>
/// Interface for read-only operations in a repository, providing methods to retrieve single entities.
/// </summary>
public interface IReadRepository
{
    /// <summary>
    /// Returns the first entity that matches the specified predicate or null if no such entity is found.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <returns>The first entity that matches the predicate, or null.</returns>
    T? FirstOrDefault<T>(Expression<Func<T, bool>> predicate) where T : class;

    /// <summary>
    /// Returns the first entity in the sequence or null if no such entity is found.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <returns>The first entity in the sequence, or null.</returns>
    T? FirstOrDefault<T>() where T : class;
}