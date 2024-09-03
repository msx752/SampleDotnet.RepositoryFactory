namespace SampleDotnet.RepositoryFactory.Interfaces.Core;

public interface IDbContextManager : IDisposable
{
    TDbContext CreateDbContext<TDbContext>() where TDbContext : DbContext;

    DbContext[] CachedDbContexts();
}