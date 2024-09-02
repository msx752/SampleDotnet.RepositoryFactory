namespace SampleDotnet.RepositoryFactory.Tests.Cases.Core.Entities.UnitOfWorkTests.UnitOfWorkModels.DbContexts;

internal class ThirdDbContext : DbContext
{
    public DbSet<ThirdDbEntity> ThirdDbEntity { get; set; }

    public ThirdDbContext(DbContextOptions<ThirdDbContext> options)
        : base(options)
    {
    }
}