namespace SampleDotnet.RepositoryFactory.Tests.TestModels.DbContexts;

public class TestApplicationDbContext : DbContext
{
    public DbSet<TestUserEntity> UserEntity { get; set; }

    public TestApplicationDbContext(DbContextOptions<TestApplicationDbContext> options)
        : base(options)
    {
    }
}