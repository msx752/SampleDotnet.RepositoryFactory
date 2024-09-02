using MassTransit;

namespace SampleDotnet.RepositoryFactory.Tests.TestModels.Sagas
{
    public class TransactionState : SagaStateMachineInstance, ISagaVersion
    {
        public Guid CorrelationId { get; set; }
        public string CurrentState { get; set; }
        public Guid TransactionId { get; set; }
        public decimal PaymentAmount { get; set; }
        public List<SagaCartItem> CartItems { get; set; }
        public DateTime? Timestamp { get; set; }
        public int Version { get; set; }
    }

}
