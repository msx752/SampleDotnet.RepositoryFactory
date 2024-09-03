namespace SampleDotnet.RepositoryFactory.Tests.Cases.Infrastructure.DbContexts.DbContextModels.DbContexts
{
    public class TestDisposeDbContext : DbContext
    {
        public DbSet<TestDisposeEntity> DiposeEntity { get; set; }

        public TestDisposeDbContext(DbContextOptions<TestDisposeDbContext> options)
            : base(options)
        {
        }
    }
}