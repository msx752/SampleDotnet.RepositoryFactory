using SampleDotnet.RepositoryFactory.Database;

namespace SampleDotnet.RepositoryFactory;

/// <summary>
/// Extension methods for adding DbContext factory and UnitOfWork to the service collection.
/// </summary>
public static class RepositoryExtensions
{
    /// <summary>
    /// Adds a repository factory and associated services to the service collection with the specified service lifetime.
    /// Ensures that only one instance of each service type is registered at a time by removing any existing registrations.
    /// </summary>
    /// <param name="serviceCollection">The service collection to which the services will be added.</param>
    /// <param name="lifetime">The lifetime of the services to add (e.g., Scoped, Transient, Singleton).</param>
    /// <returns>The modified service collection.</returns>
    public static IServiceCollection AddRepositoryFactory(this IServiceCollection serviceCollection, ServiceLifetime lifetime)
    {
        // List of service types that need to be registered.
        Type[] serviceTypes = new Type[]
        {
            typeof(IUnitOfWork),
            typeof(IDbContextManager),
            typeof(IRepositoryFactory),
            typeof(ITransactionManager)
        };

        // Loop through the service collection in reverse order to remove existing registrations safely.
        // This ensures that we do not modify the collection while iterating forward, which could cause errors.
        for (int i = serviceCollection.Count - 1; i >= 0; i--)
        {
            var serviceDescriptor = serviceCollection[i];

            // Check if the current service descriptor is one of the service types we intend to register.
            if (Array.Exists(serviceTypes, type => type == serviceDescriptor.ServiceType))
            {
                // Remove the existing service registration to avoid conflicts and ensure the new service is registered properly.
                serviceCollection.RemoveAt(i);
            }
        }

        // Register a factory for creating instances of IUnitOfWork with the specified lifetime.
        // This factory manually constructs the necessary dependencies (DbContextManager, RepositoryFactory, TransactionManager)
        // to ensure that each IUnitOfWork instance is created with the correct dependencies.
        serviceCollection.Add(new ServiceDescriptor(typeof(IUnitOfWork), x =>
        {
            IDbContextManager dbContextManager = new DbContextManager(x);
            IRepositoryFactory repositoryFactory = new Repositories.Factories.RepositoryFactory();
            ITransactionManager transactionManager = new TransactionManager(dbContextManager);

            return new UnitOfWork(dbContextManager, repositoryFactory, transactionManager);
        }, lifetime));

        return serviceCollection;
    }

}