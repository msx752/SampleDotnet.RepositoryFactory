namespace SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IUnitOfWork _unitOfWork;

        public PaymentService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task ProcessPayment(Guid transactionId, decimal amount)
        {
            using var repo = _unitOfWork.CreateRepository<PaymentDbContext>();
            var payment = new PaymentEntity
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
            using var repo = _unitOfWork.CreateRepository<PaymentDbContext>();
            var payment = await repo.FirstOrDefaultAsync<PaymentEntity>(p => p.TransactionId == transactionId);
            if (payment != null)
            {
                payment.Status = PaymentStatus.Cancelled;
                repo.Update(payment);
                await _unitOfWork.SaveChangesAsync();
            }
        }
    }
}