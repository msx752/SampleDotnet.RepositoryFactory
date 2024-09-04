namespace SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.DbContexts;

// PaymentDbContext class
public class TestPaymentDbContext : DbContext
{
    public TestPaymentDbContext(DbContextOptions<TestPaymentDbContext> options) : base(options)
    {
    }

    public DbSet<TestPaymentEntity> Payments { get; set; }
}