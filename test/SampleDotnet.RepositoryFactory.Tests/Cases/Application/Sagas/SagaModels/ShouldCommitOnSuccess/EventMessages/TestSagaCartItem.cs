namespace SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.ShouldCommitOnSuccess.EventMessages;

public class TestSagaCartItem
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}