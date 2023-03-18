﻿namespace SampleDotnet.RepositoryFactory.Interfaces;

public interface IRepository : IDisposable
{
    DatabaseFacade Database => CurrentDbContext.Database;
    Type DbContextType => CurrentDbContext.GetType();

    protected abstract DbContext CurrentDbContext { get; }

    int SaveChanges();

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface IRepository<TDbContext> : IRepository
    where TDbContext : DbContext
{
    IQueryable<T> AsQueryable<T>() where T : class;

    void Delete<T>(T entity) where T : class;

    void Delete<T>(params T[] entities) where T : class;

    void Delete<T>(IEnumerable<T> entities) where T : class;

    T? Find<T>(params object[] keyValues) where T : class;

    ValueTask<T?> FindAsync<T>(object[] keyValues, CancellationToken cancellationToken = default) where T : class;

    T? FirstOrDefault<T>(Expression<Func<T, bool>> predicate) where T : class;

    Task<T?> FirstOrDefaultAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) where T : class;

    T? GetById<T>(object id) where T : class;

    ValueTask<T?> GetByIdAsync<T>(object id, CancellationToken cancellationToken = default) where T : class;

    void Insert<T>(T entity) where T : class;

    void Insert<T>(params T[] entities) where T : class;

    void Insert<T>(IEnumerable<T> entities) where T : class;

    ValueTask<EntityEntry<T>> InsertAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class;

    Task InsertAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default) where T : class;

    Task InsertAsync<T>(T[] entities, CancellationToken cancellationToken = default) where T : class;

    void Update<T>(T entity) where T : class;

    void Update<T>(params T[] entities) where T : class;

    void Update<T>(IEnumerable<T> entities) where T : class;

    IQueryable<T> Where<T>(Expression<Func<T, bool>> predicate) where T : class;
}