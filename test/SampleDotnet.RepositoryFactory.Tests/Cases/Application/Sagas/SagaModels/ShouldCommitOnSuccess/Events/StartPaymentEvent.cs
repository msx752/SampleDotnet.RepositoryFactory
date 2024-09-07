namespace SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.ShouldCommitOnSuccess.Events;

public record StartPaymentEvent(Guid CorrelationId, decimal Amount) : CorrelatedBy<Guid>;