using SampleDotnet.RepositoryFactory.Exceptions;

namespace SampleDotnet.RepositoryFactory.Interfaces.Core;

public interface ITransactionManager : ITransactionSave, IAsyncRollback, IDisposable
{
    /// <summary>
    /// Provides details of any exception that occurred during the save operation.
    /// </summary>
    SaveChangesExceptionDetail? SaveChangesException { get; }

    /// <summary>
    /// Indicates whether a database concurrency exception was encountered.
    /// </summary>
    bool IsDbConcurrencyExceptionThrown { get; }
}