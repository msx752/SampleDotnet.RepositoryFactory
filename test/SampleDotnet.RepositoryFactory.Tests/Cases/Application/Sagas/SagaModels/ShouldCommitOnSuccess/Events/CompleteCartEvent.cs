namespace SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.ShouldCommitOnSuccess.Events;

public record CompleteCartEvent(Guid CorrelationId) : CorrelatedBy<Guid>;