namespace SampleDotnet.RepositoryFactory.Tests.TestModels.Sagas
{
    // PaymentDbContext class
    public class PaymentDbContext : DbContext
    {
        public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options) { }

        public DbSet<Payment> Payments { get; set; }
    }

}
