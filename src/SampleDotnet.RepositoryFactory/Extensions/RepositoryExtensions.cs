namespace SampleDotnet.RepositoryFactory;

/// <summary>
/// Extension methods for adding DbContext factory and UnitOfWork to the service collection.
/// </summary>
public static class RepositoryExtensions
{
    /// <summary>
    /// Adds a DbContext factory and a UnitOfWork to the service collection with the specified service lifetime.
    /// </summary>
    /// <typeparam name="TContext">The type of the DbContext to add.</typeparam>
    /// <param name="serviceCollection">The service collection to which the services will be added.</param>
    /// <param name="optionsAction">An optional action to configure the DbContext options.</param>
    /// <param name="lifetime">The lifetime of the services to add. Default is Singleton.</param>
    /// <returns>The modified service collection.</returns>
    public static IServiceCollection AddDbContextFactoryWithUnitOfWork<TContext>(
        this IServiceCollection serviceCollection,
        Action<DbContextOptionsBuilder>? optionsAction = null,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TContext : DbContext
    {
        // Adds a factory for creating instances of the specified DbContext type with the provided options.
        Microsoft.Extensions.DependencyInjection.EntityFrameworkServiceCollectionExtensions.AddDbContextFactory<TContext, Microsoft.EntityFrameworkCore.Internal.DbContextFactory<TContext>>(serviceCollection, optionsAction, lifetime);

        // Adds the UnitOfWork as a scoped service if it hasn't already been added.
        Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions.TryAddScoped<IUnitOfWork, UnitOfWork>(serviceCollection);

        return serviceCollection;
    }
}