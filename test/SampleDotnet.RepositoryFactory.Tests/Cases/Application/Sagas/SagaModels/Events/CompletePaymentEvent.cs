namespace SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.Events;

public record CompletePaymentEvent(Guid CorrelationId) : CorrelatedBy<Guid>;