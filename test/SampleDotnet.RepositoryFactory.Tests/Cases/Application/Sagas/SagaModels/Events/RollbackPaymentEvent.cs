namespace SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.Events;

public record RollbackPaymentEvent(Guid CorrelationId) : CorrelatedBy<Guid>;