namespace SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.Interfaces
{
    public interface IPaymentService
    {
        Task ProcessPayment(Guid transactionId, decimal amount);

        Task RollbackPayment(Guid transactionId);
    }
}