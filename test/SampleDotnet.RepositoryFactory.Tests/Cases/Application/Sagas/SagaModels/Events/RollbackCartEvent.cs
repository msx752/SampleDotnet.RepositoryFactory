namespace SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.Events;

public record RollbackCartEvent(Guid CorrelationId) : CorrelatedBy<Guid>;