namespace SampleDotnet.RepositoryFactory.Interfaces;

public interface IRepository<TDbContext> : IDisposable
    where TDbContext : DbContext
{
    DatabaseFacade Database { get; }

    IQueryable<T> AsQueryable<T>() where T : class;

    void Delete<T>(T entity) where T : class;

    void Delete<T>(params T[] entities) where T : class;

    void Delete<T>(IEnumerable<T> entities) where T : class;

    T? Find<T>(params object[] keyValues) where T : class;

    T? FirstOrDefault<T>(Expression<Func<T, bool>> predicate) where T : class;

    Task<T?> FirstOrDefaultAsync<T>(Expression<Func<T, bool>> predicate) where T : class;

    T? GetById<T>(object id) where T : class;

    ValueTask<T?> GetByIdAsync<T>(object id) where T : class;

    void Insert<T>(T entity) where T : class;

    void Insert<T>(params T[] entities) where T : class;

    void Insert<T>(IEnumerable<T> entities) where T : class;

    ValueTask<EntityEntry<T>> InsertAsync<T>(T entity) where T : class;

    Task InsertAsync<T>(IEnumerable<T> entities) where T : class;

    Task InsertAsync<T>(params T[] entities) where T : class;

    int SaveChanges();

    Task<int> SaveChangesAsync();

    void Update<T>(T entity) where T : class;

    void Update<T>(params T[] entities) where T : class;

    void Update<T>(IEnumerable<T> entities) where T : class;

    IQueryable<T> Where<T>(Expression<Func<T, bool>> predicate) where T : class;
}