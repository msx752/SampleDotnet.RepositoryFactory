namespace SampleDotnet.RepositoryFactory.Interfaces;

/// <summary>
/// Represents a generic repository interface that provides basic CRUD operations.
/// </summary>
public interface IRepository : IDisposable
{
}

/// <summary>
/// Represents a generic repository interface for a specific <see cref="DbContext"/> type, providing additional functionality.
/// </summary>
/// <typeparam name="TDbContext">The type of the DbContext.</typeparam>
public interface IRepository<TDbContext> :
    IRepository,
    IReadRepository,
    IReadNativeRepository,
    IReadAsyncRepository,
    IWriteRepository,
    IWriteNativeRepository,
    IWriteAsyncRepository,
    IQueryableRepository
    where TDbContext : DbContext
{
}