using MassTransit;

namespace SampleDotnet.RepositoryFactory.Tests.TestModels.Sagas
{
    public record CompleteCart(Guid CorrelationId) : CorrelatedBy<Guid>;

}
