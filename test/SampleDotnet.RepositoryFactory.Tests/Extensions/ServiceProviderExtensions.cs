namespace SampleDotnet.RepositoryFactory.Tests.Extensions;

public static class ServiceProviderExtensions
{
    public static void EnsureDatabaseExists<TDbContext>(this IServiceProvider provider) where TDbContext : DbContext
    {
        var dbContextFactory = provider.GetRequiredService<IDbContextFactory<TDbContext>>();
        using (var context = dbContextFactory.CreateDbContext())
            context.Database.EnsureCreated();
    }
}