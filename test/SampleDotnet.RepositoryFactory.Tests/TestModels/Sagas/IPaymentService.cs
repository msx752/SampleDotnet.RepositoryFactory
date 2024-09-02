namespace SampleDotnet.RepositoryFactory.Tests.TestModels.Sagas
{
    public interface IPaymentService
    {
        Task ProcessPayment(Guid transactionId, decimal amount);
        Task RollbackPayment(Guid transactionId);
    }

}
