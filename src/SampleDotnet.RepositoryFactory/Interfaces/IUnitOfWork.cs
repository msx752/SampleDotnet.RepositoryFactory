namespace SampleDotnet.RepositoryFactory.Interfaces;

public interface IUnitOfWork : IDisposable
{
    bool IsDbConcurrencyExceptionThrown { get; }

    SaveChangesExceptionDetail? SaveChangesException { get; }

    IRepository<TDbContext> CreateRepository<TDbContext>(/*TransactionScopeOption transactionScopeOption = TransactionScopeOption.RequiresNew, System.Transactions.IsolationLevel isolationLevel = System.Transactions.IsolationLevel.ReadCommitted*/) where TDbContext : DbContext;

    bool SaveChanges();

    Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default);
}