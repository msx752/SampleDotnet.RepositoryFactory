namespace SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.Entities;

// Payment entity
public class TestPaymentEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    public Guid TransactionId { get; set; }
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; }
}