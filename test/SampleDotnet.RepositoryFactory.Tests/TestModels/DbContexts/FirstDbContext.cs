namespace SampleDotnet.RepositoryFactory.Tests.TestModels.DbContexts;

// Represents the first database context for Entity Framework Core.
internal class FirstDbContext : DbContext
{
    public FirstDbContext(DbContextOptions<FirstDbContext> options)
        : base(options)
    {
    }

    public DbSet<FirstDbEntity> FirstDbEntity { get; set; }
}