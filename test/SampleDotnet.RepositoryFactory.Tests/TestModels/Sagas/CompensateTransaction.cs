using MassTransit;

namespace SampleDotnet.RepositoryFactory.Tests.TestModels.Sagas
{
    public record CompensateTransaction(Guid CorrelationId) : CorrelatedBy<Guid>;

}
