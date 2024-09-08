using SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.ShouldCommitOnSuccess.DbContexts;
using SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.ShouldCommitOnSuccess.Entities;
using SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.ShouldCommitOnSuccess.Enums;
using SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.ShouldCommitOnSuccess.Interfaces;

namespace SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.ShouldRollbackOnFailure.Services;

public class TestCartService_Fail : ITestCartService
{
    private readonly IUnitOfWork _unitOfWork;

    public TestCartService_Fail(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task ProcessCart(Guid transactionId, List<TestCartItemEntity> items)
    {
        using var repo = _unitOfWork.CreateRepository<TestCartDbContext>();
        var cart = new TestCartEntity
        {
            TransactionId = transactionId,
            Items = items,
            Status = CartStatus.Pending
        };

        await repo.AddAsync(cart);
        throw new Exception("operation halt, CompensateTransactionEvent need to be called");
    }

    public async Task RollbackCart(Guid transactionId)
    {
        using var repo = _unitOfWork.CreateRepository<TestCartDbContext>();
        var cart = await repo.FirstOrDefaultAsync<TestCartEntity>(c => c.TransactionId == transactionId);
        if (cart != null)
        {
            cart.Status = CartStatus.Cancelled;
            repo.Update(cart);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}