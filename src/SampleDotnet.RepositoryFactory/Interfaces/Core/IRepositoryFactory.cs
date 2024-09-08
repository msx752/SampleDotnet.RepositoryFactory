namespace SampleDotnet.RepositoryFactory.Interfaces.Core;

public interface IRepositoryFactory : IDisposable
{
    /// <summary>
    /// Creates a repository for the specified DbContext instance.
    /// </summary>
    /// <typeparam name="TDbContext">The type of the DbContext.</typeparam>
    /// <param name="dbContext">The DbContext instance to create the repository for.</param>
    /// <returns>A repository for the specified DbContext.</returns>
    IRepository<TDbContext> CreateRepository<TDbContext>(TDbContext dbContext) where TDbContext : DbContext;
}