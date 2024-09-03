namespace SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.Events;

public record StartTransactionEvent(Guid CorrelationId, decimal PaymentAmount, List<SagaCartItem> CartItems) : CorrelatedBy<Guid>;