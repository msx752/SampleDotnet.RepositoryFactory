using SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels;
using SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.Consumers;
using SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.DbContexts;
using SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.Entities;
using SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.Enums;
using SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.EventMessages;
using SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.Events;
using SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.Interfaces;
using SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.Services;
namespace SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.Services;

public class TestPaymentService : ITestPaymentService
{
    private readonly IUnitOfWork _unitOfWork;

    public TestPaymentService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task ProcessPayment(Guid transactionId, decimal amount)
    {
        using var repo = _unitOfWork.CreateRepository<TestPaymentDbContext>();
        var payment = new TestPaymentEntity
        {
            TransactionId = transactionId,
            Amount = amount,
            Status = PaymentStatus.Pending
        };

        await repo.InsertAsync(payment);
        await _unitOfWork.SaveChangesAsync();

        payment.Status = PaymentStatus.Completed;
        repo.Update(payment);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task RollbackPayment(Guid transactionId)
    {
        using var repo = _unitOfWork.CreateRepository<TestPaymentDbContext>();
        var payment = await repo.FirstOrDefaultAsync<TestPaymentEntity>(p => p.TransactionId == transactionId);
        if (payment != null)
        {
            payment.Status = PaymentStatus.Cancelled;
            repo.Update(payment);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}