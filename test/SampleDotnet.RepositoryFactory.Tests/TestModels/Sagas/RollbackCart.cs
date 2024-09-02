using MassTransit;

namespace SampleDotnet.RepositoryFactory.Tests.TestModels.Sagas
{
    public record RollbackCart(Guid CorrelationId) : CorrelatedBy<Guid>;

}
