namespace SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.Entities;

// CartItem entity
public class CartItemEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public Guid? CartId { get; set; }
}