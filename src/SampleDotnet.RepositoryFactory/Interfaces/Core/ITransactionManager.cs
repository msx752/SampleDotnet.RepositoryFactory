namespace SampleDotnet.RepositoryFactory.Interfaces.Core;

public interface ITransactionManager : ITransactionSave, IAsyncRollback, IDisposable
{
    SaveChangesExceptionDetail? SaveChangesException { get; }
    bool IsDbConcurrencyExceptionThrown { get; }
}