public static class RepositoryExtensions
{
    public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddDbContextFactoryWithUnitOfWork<TContext>(this Microsoft.Extensions.DependencyInjection.IServiceCollection serviceCollection, Action<DbContextOptionsBuilder>? optionsAction = null, Microsoft.Extensions.DependencyInjection.ServiceLifetime lifetime = Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton)
        where TContext : DbContext
    {
        Microsoft.Extensions.DependencyInjection.EntityFrameworkServiceCollectionExtensions.AddDbContextFactory<TContext, Microsoft.EntityFrameworkCore.Internal.DbContextFactory<TContext>>(serviceCollection, optionsAction, lifetime);
        Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions.TryAddScoped<IUnitOfWork, UnitOfWork>(serviceCollection);

        return serviceCollection;
    }
}