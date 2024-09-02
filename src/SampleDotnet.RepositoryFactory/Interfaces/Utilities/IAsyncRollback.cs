namespace SampleDotnet.RepositoryFactory.Interfaces.Utilities;

/// <summary>
/// Defines a method for asynchronously rolling back changes in a DbContext.
/// </summary>
public interface IAsyncRollback
{
    /// <summary>
    /// Asynchronously rolls back all changes made in the specified DbContext.
    /// </summary>
    /// <param name="context">The DbContext instance in which to rollback changes.</param>
    /// <param name="overrideDetectChanges">
    /// If true, overrides the DbContext's AutoDetectChanges setting to manually detect changes before rolling back.
    /// </param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous operation of rolling back changes.</returns>
    Task RollbackChangesAsync(DbContext context, bool overrideDetectChanges = false, CancellationToken cancellationToken = default);
}