using MassTransit;

namespace SampleDotnet.RepositoryFactory.Tests.TestModels.Sagas
{
    public record StartTransaction(Guid CorrelationId, decimal PaymentAmount, List<SagaCartItem> CartItems) : CorrelatedBy<Guid>;

}
