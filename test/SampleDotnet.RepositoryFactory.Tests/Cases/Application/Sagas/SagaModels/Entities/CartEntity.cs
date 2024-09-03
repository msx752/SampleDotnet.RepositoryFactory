namespace SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.Entities;

// Cart entity
public class CartEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    public Guid TransactionId { get; set; }
    public List<CartItemEntity> Items { get; set; }
    public CartStatus Status { get; set; }
}