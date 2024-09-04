namespace SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.ShouldCommitOnSuccess.Interfaces
{
    public interface ITestPaymentService
    {
        Task ProcessPayment(Guid transactionId, decimal amount);

        Task RollbackPayment(Guid transactionId);
    }
}