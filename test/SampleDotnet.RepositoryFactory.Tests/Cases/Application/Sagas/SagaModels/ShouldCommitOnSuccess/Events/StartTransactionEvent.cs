using SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.ShouldCommitOnSuccess.EventMessages;

namespace SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.ShouldCommitOnSuccess.Events;

public record StartTransactionEvent(Guid CorrelationId, decimal PaymentAmount, List<TestSagaCartItem> CartItems) : CorrelatedBy<Guid>;