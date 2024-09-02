namespace SampleDotnet.RepositoryFactory.Tests.TestModels.Sagas
{
    public interface ICartService
    {
        Task ProcessCart(Guid transactionId, List<CartItem> items);
        Task RollbackCart(Guid transactionId);
    }

}
