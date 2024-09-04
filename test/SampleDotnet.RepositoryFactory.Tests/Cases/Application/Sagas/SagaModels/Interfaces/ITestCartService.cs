namespace SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.Interfaces
{
    public interface ITestCartService
    {
        Task ProcessCart(Guid transactionId, List<TestCartItemEntity> items);

        Task RollbackCart(Guid transactionId);
    }
}