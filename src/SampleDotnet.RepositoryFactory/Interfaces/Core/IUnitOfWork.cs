using SampleDotnet.RepositoryFactory.Exceptions;

namespace SampleDotnet.RepositoryFactory.Interfaces.Core;

// Represents the Unit of Work pattern for managing transactions and repositories
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Creates a repository for the specified DbContext type.
    /// </summary>
    /// <typeparam name="TDbContext">The type of the DbContext.</typeparam>
    /// <returns>A repository for the specified DbContext.</returns>
    IRepository<TDbContext> CreateRepository<TDbContext>() where TDbContext : DbContext;

    /// <summary>
    /// Saves all changes made in the current unit of work to the database.
    /// </summary>
    /// <returns>True if the changes were successfully saved; otherwise, false.</returns>
    bool SaveChanges();

    /// <summary>
    /// Asynchronously saves all changes made in the current unit of work to the database.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A task representing the async save operation. True if successful; otherwise, false.</returns>
    Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a value indicating whether a database concurrency exception has been thrown.
    /// </summary>
    bool IsDbConcurrencyExceptionThrown { get; }

    /// <summary>
    /// Provides details of any exception that occurred during the save operation.
    /// </summary>
    SaveChangesExceptionDetail? SaveChangesException { get; }
}