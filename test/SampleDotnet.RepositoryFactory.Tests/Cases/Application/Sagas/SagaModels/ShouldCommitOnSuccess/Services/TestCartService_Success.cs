using SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.ShouldCommitOnSuccess.DbContexts;
using SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.ShouldCommitOnSuccess.Entities;
using SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.ShouldCommitOnSuccess.Enums;
using SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.ShouldCommitOnSuccess.Interfaces;

namespace SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.ShouldCommitOnSuccess.Services;

public class TestCartService_Success : ITestCartService
{
    private readonly IUnitOfWork _unitOfWork;

    public TestCartService_Success(IUnitOfWork unitOfWork)
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
        await _unitOfWork.SaveChangesAsync();

        cart.Status = CartStatus.Completed;
        repo.Update(cart);
        await _unitOfWork.SaveChangesAsync();
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