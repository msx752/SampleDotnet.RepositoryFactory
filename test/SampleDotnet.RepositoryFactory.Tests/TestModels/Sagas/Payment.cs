namespace SampleDotnet.RepositoryFactory.Tests.TestModels.Sagas
{
    // Payment entity
    public class Payment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        public Guid TransactionId { get; set; }
        public decimal Amount { get; set; }
        public PaymentStatus Status { get; set; }
    }

}
