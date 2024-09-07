namespace SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.ShouldCommitOnSuccess.Events;

public record RollbackCartEvent(Guid CorrelationId) : CorrelatedBy<Guid>;