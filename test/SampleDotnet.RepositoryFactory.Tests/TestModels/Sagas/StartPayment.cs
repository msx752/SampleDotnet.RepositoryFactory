using MassTransit;

namespace SampleDotnet.RepositoryFactory.Tests.TestModels.Sagas
{
    public record StartPayment(Guid CorrelationId, decimal Amount) : CorrelatedBy<Guid>;

}
