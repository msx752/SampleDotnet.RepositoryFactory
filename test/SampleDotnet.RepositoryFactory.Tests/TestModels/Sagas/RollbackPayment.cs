using MassTransit;

namespace SampleDotnet.RepositoryFactory.Tests.TestModels.Sagas
{
    public record RollbackPayment(Guid CorrelationId) : CorrelatedBy<Guid>;

}
