namespace SampleDotnet.RepositoryFactory.Tests.Cases.Core.Entities.UnitOfWorkTests.DbContexts;

internal class SecondDbContext : DbContext
{
    public SecondDbContext(DbContextOptions<SecondDbContext> options)
        : base(options)
    {
    }

    public DbSet<SecondDbEntity> SecondDbEntity { get; set; }
}