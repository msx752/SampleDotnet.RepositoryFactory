namespace SampleDotnet.RepositoryFactory.Interfaces.Repositories;

/// <summary>
/// Interface for native write operations in a repository, providing methods to update and delete entities.
/// </summary>
public interface IWriteNativeRepository
{
    /// <summary>
    /// Updates the specified entity in the repository.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    void Update(object entity);

    /// <summary>
    /// Updates a range of entities in the repository.
    /// </summary>
    /// <param name="entities">The entities to update.</param>
    void UpdateRange(params object[] entities);

    /// <summary>
    /// Deletes the specified entity from the repository.
    /// </summary>
    /// <param name="entity">The entity to delete.</param>
    void Delete(object entity);

    /// <summary>
    /// Deletes a range of entities from the repository.
    /// </summary>
    /// <param name="entities">The entities to delete.</param>
    void DeleteRange(params object[] entities);
}