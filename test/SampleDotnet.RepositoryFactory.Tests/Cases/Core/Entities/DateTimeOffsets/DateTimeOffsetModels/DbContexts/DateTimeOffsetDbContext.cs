namespace SampleDotnet.RepositoryFactory.Tests.Cases.Core.Entities.UnitOfWorkTests.UnitOfWorkModels.DbContexts;

internal class DateTimeOffsetDbContext : DbContext
{
    public DbSet<DateTimeOffsetDbEntity> DateTimeOffsetDbEntity { get; set; }

    public DateTimeOffsetDbContext(DbContextOptions<DateTimeOffsetDbContext> options)
        : base(options)
    {
    }
}