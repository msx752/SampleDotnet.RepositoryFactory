namespace SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.Events;

public record CompensateTransactionEvent(Guid CorrelationId) : CorrelatedBy<Guid>;