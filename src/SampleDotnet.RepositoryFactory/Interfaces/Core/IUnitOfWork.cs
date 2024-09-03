namespace SampleDotnet.RepositoryFactory.Interfaces.Core;

public interface IUnitOfWork : IDisposable
{
    IRepository<TDbContext> CreateRepository<TDbContext>() where TDbContext : DbContext;

    bool SaveChanges();

    Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default);

    bool IsDbConcurrencyExceptionThrown { get; }
    SaveChangesExceptionDetail? SaveChangesException { get; }
}