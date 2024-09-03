namespace SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.Interfaces
{
    public interface ICartService
    {
        Task ProcessCart(Guid transactionId, List<CartItemEntity> items);

        Task RollbackCart(Guid transactionId);
    }
}