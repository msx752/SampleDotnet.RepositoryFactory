namespace SampleDotnet.RepositoryFactory.Interfaces;

/// <summary>
/// Represents a generic repository interface that provides basic CRUD operations.
/// </summary>
public interface IRepository : IDisposable
{
    DbContext Db { get; }

    EntityEntry Add(object entity);

    ValueTask<EntityEntry> AddAsync(object entity, CancellationToken cancellationToken = default);

    void AddRange(params object[] entities);

    Task AddRangeAsync(IEnumerable<object> entities, CancellationToken cancellationToken = default);

    EntityEntry Remove(object entity);

    void RemoveRange(params object[] entities);

    EntityEntry Update(object entity);

    void UpdateRange(params object[] entities);

    DbSet<TEntity> Set<TEntity>() where TEntity : class;
}

/// <summary>
/// Represents a generic repository interface for a specific <see cref="DbContext"/> type, providing additional functionality.
/// </summary>
/// <typeparam name="TDbContext">The type of the DbContext.</typeparam>
public interface IRepository<TDbContext> : IRepository where TDbContext : DbContext
{
    IQueryable<TEntity> AsNoTracking<TEntity>() where TEntity : class;

    IQueryable<TEntity> AsQueryable<TEntity>() where TEntity : class;

    TEntity? Find<TEntity>(params object[] keyValues) where TEntity : class;

    ValueTask<TEntity?> FindAsync<TEntity>(object[] keyValues, CancellationToken cancellationToken = default) where TEntity : class;

    TEntity? FirstOrDefault<TEntity>(Expression<Func<TEntity, bool>> predicate) where TEntity : class;

    TEntity? FirstOrDefault<TEntity>() where TEntity : class;

    Task<TEntity?> FirstOrDefaultAsync<TEntity>(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default) where TEntity : class;

    Task<TEntity?> FirstOrDefaultAsync<TEntity>(CancellationToken cancellationToken = default) where TEntity : class;

    IQueryable<TEntity> Where<TEntity>(Expression<Func<TEntity, bool>> predicate) where TEntity : class;
}