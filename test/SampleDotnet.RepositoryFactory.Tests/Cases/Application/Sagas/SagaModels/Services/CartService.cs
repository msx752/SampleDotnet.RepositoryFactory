﻿namespace SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.Services
{
    public class CartService : ICartService
    {
        private readonly IUnitOfWork _unitOfWork;

        public CartService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task ProcessCart(Guid transactionId, List<CartItemEntity> items)
        {
            using var repo = _unitOfWork.CreateRepository<CartDbContext>();
            var cart = new CartEntity
            {
                TransactionId = transactionId,
                Items = items,
                Status = CartStatus.Pending
            };

            await repo.InsertAsync(cart);
            await _unitOfWork.SaveChangesAsync();

            cart.Status = CartStatus.Completed;
            repo.Update(cart);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task RollbackCart(Guid transactionId)
        {
            using var repo = _unitOfWork.CreateRepository<CartDbContext>();
            var cart = await repo.FirstOrDefaultAsync<CartEntity>(c => c.TransactionId == transactionId);
            if (cart != null)
            {
                cart.Status = CartStatus.Cancelled;
                repo.Update(cart);
                await _unitOfWork.SaveChangesAsync();
            }
        }
    }
}