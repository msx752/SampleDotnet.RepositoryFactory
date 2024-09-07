using SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.ShouldCommitOnSuccess.EventMessages;

namespace SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.ShouldCommitOnSuccess.Events;

public record StartCartEvent(Guid CorrelationId, List<TestSagaCartItem> Items) : CorrelatedBy<Guid>;