namespace SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.DbContexts;

// PaymentDbContext class
public class PaymentDbContext : DbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options)
    {
    }

    public DbSet<PaymentEntity> Payments { get; set; }
}