namespace SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.Events;

public record CompleteCartEvent(Guid CorrelationId) : CorrelatedBy<Guid>;