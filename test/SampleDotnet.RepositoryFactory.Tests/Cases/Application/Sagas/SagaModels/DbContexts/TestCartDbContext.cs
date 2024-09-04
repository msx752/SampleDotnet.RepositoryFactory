namespace SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.DbContexts;

// CartDbContext class
public class TestCartDbContext : DbContext
{
    public TestCartDbContext(DbContextOptions<TestCartDbContext> options) : base(options)
    {
    }

    public DbSet<TestCartEntity> Carts { get; set; }
    public DbSet<TestCartItemEntity> CartItems { get; set; }
}