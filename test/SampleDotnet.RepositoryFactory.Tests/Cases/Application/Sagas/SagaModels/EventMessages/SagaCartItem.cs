namespace SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.EventMessages;

public class SagaCartItem
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}