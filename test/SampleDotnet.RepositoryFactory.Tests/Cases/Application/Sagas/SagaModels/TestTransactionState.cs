using SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels;
using SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.Consumers;
using SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.DbContexts;
using SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.Entities;
using SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.Enums;
using SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.EventMessages;
using SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.Events;
using SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.Interfaces;
using SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.Services;

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