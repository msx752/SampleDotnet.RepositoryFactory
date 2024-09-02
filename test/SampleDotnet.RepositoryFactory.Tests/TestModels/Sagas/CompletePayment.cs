using MassTransit;

namespace SampleDotnet.RepositoryFactory.Tests.TestModels.Sagas
{
    public record CompletePayment(Guid CorrelationId) : CorrelatedBy<Guid>;

}
