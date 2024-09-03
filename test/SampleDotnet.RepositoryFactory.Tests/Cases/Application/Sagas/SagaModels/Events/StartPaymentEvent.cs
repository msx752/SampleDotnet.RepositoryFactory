namespace SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.Events;

public record StartPaymentEvent(Guid CorrelationId, decimal Amount) : CorrelatedBy<Guid>;