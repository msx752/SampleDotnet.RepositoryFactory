namespace SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels
{
    public class TestTransactionState : SagaStateMachineInstance, ISagaVersion
    {
        public Guid CorrelationId { get; set; }
        public string CurrentState { get; set; }
        public Guid TransactionId { get; set; }
        public decimal PaymentAmount { get; set; }
        public List<TestSagaCartItem> CartItems { get; set; }
        public DateTime? Timestamp { get; set; }
        public int Version { get; set; }
    }
}