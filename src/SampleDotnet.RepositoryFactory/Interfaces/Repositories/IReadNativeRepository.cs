namespace SampleDotnet.RepositoryFactory.Interfaces.Repositories;

/// <summary>
/// Interface for native read operations in a repository, providing methods to retrieve entities by key values.
/// </summary>
public interface IReadNativeRepository
{
    /// <summary>
    /// Retrieves an entity by its primary key.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="id">The primary key of the entity.</param>
    /// <returns>The entity found, or null.</returns>
    T? GetById<T>(object id) where T : class;

    /// <summary>
    /// Finds an entity by its composite key values.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="keyValues">An array of key values for the entity.</param>
    /// <returns>The entity found, or null.</returns>
    T? Find<T>(params object[] keyValues) where T : class;
}