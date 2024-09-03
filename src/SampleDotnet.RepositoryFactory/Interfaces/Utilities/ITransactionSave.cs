namespace SampleDotnet.RepositoryFactory.Interfaces.Utilities;

/// <summary>
/// Defines a method for asynchronously saving changes in a transactional context.
/// </summary>
public interface ITransactionSave
{
    /// <summary>
    /// Asynchronously saves all changes made within the current transaction context.
    /// </summary>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous save operation, with a boolean indicating whether the save was successful.</returns>
    Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default);
}