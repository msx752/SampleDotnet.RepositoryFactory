namespace SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.ShouldCommitOnSuccess.Events;

public record CompensateTransactionEvent(Guid CorrelationId) : CorrelatedBy<Guid>;