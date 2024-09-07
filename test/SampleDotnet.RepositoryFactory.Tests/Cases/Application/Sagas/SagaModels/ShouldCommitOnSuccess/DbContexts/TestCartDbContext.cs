using SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.ShouldCommitOnSuccess.Entities;

namespace SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.ShouldCommitOnSuccess.DbContexts;

// CartDbContext class
public class TestCartDbContext : DbContext
{
    public TestCartDbContext(DbContextOptions<TestCartDbContext> options) : base(options)
    {
    }

    public DbSet<TestCartEntity> Carts { get; set; }
    public DbSet<TestCartItemEntity> CartItems { get; set; }
}