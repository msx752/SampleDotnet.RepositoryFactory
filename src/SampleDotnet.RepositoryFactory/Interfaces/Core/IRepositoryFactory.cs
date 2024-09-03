namespace SampleDotnet.RepositoryFactory.Interfaces.Core;

public interface IRepositoryFactory : IDisposable
{
    IRepository<TDbContext> CreateRepository<TDbContext>(TDbContext dbContext) where TDbContext : DbContext;
}