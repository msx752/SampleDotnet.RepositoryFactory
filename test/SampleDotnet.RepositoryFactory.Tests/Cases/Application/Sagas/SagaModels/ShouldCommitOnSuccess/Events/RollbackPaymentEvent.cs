namespace SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.ShouldCommitOnSuccess.Events;

public record RollbackPaymentEvent(Guid CorrelationId) : CorrelatedBy<Guid>;