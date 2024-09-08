namespace SampleDotnet.RepositoryFactory.Interfaces.Core;

public interface IDbContextManager : IDisposable
{
    /// <summary>
    /// Creates a new instance of the specified DbContext type.
    /// </summary>
    /// <typeparam name="TDbContext">The type of the DbContext.</typeparam>
    /// <returns>An instance of the specified DbContext.</returns>
    TDbContext CreateDbContext<TDbContext>() where TDbContext : DbContext;

    /// <summary>
    /// Retrieves cached DbContext instances that are being managed.
    /// </summary>
    /// <returns>An array of cached DbContext instances.</returns>
    DbContext[] CachedDbContexts();
}