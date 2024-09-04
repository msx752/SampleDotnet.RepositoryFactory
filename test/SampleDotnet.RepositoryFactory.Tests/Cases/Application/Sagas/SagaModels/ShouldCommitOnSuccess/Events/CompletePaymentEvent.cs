namespace SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.ShouldCommitOnSuccess.Events;

public record CompletePaymentEvent(Guid CorrelationId) : CorrelatedBy<Guid>;