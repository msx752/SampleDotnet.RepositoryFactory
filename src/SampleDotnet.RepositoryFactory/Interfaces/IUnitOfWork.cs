namespace SampleDotnet.RepositoryFactory.Interfaces;

public interface IUnitOfWork : IDisposable
{
    bool IsDbConcurrencyExceptionThrown { get; }

    SaveChangesExceptionDetail? SaveChangesException { get; }

    IRepository<TDbContext> CreateRepository<TDbContext>() where TDbContext : DbContext;

    bool SaveChanges();

    Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default);
}