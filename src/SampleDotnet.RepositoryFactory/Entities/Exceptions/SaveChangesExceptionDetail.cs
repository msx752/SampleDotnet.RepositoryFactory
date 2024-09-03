namespace SampleDotnet.RepositoryFactory.Entities.Exceptions;

/// <summary>
/// Represents detailed information about an exception that occurred during a SaveChanges operation.
/// </summary>
public class SaveChangesExceptionDetail
{
    /// <summary>
    /// Gets the DbContext instance where the exception was thrown.
    /// </summary>
    public DbContext? DbContext { get; }

    /// <summary>
    /// Gets the exception that was thrown during the SaveChanges operation.
    /// </summary>
    public Exception Exception { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SaveChangesExceptionDetail"/> class with the specified DbContext and exception.
    /// </summary>
    /// <param name="dbContext">The DbContext instance where the exception occurred. Can be null if the exception is not tied to a specific DbContext.</param>
    /// <param name="exception">The exception that was thrown during the SaveChanges operation.</param>
    public SaveChangesExceptionDetail(DbContext? dbContext, Exception exception)
    {
        DbContext = dbContext;
        Exception = exception;
    }
}