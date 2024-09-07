using SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.ShouldCommitOnSuccess.Entities;

namespace SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.ShouldCommitOnSuccess.DbContexts;

// PaymentDbContext class
public class TestPaymentDbContext : DbContext
{
    public TestPaymentDbContext(DbContextOptions<TestPaymentDbContext> options) : base(options)
    {
    }

    public DbSet<TestPaymentEntity> Payments { get; set; }
}