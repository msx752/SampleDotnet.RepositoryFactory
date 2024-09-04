using SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.ShouldCommitOnSuccess.Entities;

namespace SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.ShouldCommitOnSuccess.Interfaces
{
    public interface ITestCartService
    {
        Task ProcessCart(Guid transactionId, List<TestCartItemEntity> items);

        Task RollbackCart(Guid transactionId);
    }
}