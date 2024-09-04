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
    public class TestTransactionStateMachine : MassTransitStateMachine<TestTransactionState>
    {
        public TestTransactionStateMachine()
        {
            InstanceState(x => x.CurrentState);

            Event(() => StartTransactionEvent, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => CompletePaymentEvent, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => CompleteCartEvent, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => CompensateTransactionEvent, x => x.CorrelateById(context => context.Message.CorrelationId));

            Initially(
                When(StartTransactionEvent)
                    .Then(context =>
                    {
                        context.Saga.TransactionId = context.Message.CorrelationId;
                        context.Saga.PaymentAmount = context.Message.PaymentAmount;
                        context.Saga.CartItems = context.Message.CartItems;
                        context.Saga.Timestamp = DateTime.UtcNow;
                        context.Publish(new StartPaymentEvent(context.Message.CorrelationId, context.Message.PaymentAmount));
                    })
                    .TransitionTo(Processing));

            During(Processing,
                When(CompletePaymentEvent)
                    .Then(context =>
                    {
                        context.Publish(new StartCartEvent(context.Message.CorrelationId, context.Saga.CartItems));
                    }),
                When(CompleteCartEvent)
                    .Then(context =>
                    {
                        context.Saga.CurrentState = "Completed";
                    })
                    .TransitionTo(Completed),
                When(CompensateTransactionEvent)
                    .ThenAsync(async context =>
                    {
                        await context.Publish(new RollbackPaymentEvent(context.Message.CorrelationId));
                        await context.Publish(new RollbackCartEvent(context.Message.CorrelationId));
                    })
                    .TransitionTo(Rolledback));

            SetCompletedWhenFinalized();
        }

        public State Processing { get; private set; }
        public State Completed { get; private set; }
        public State Rolledback { get; private set; }

        public Event<StartTransactionEvent> StartTransactionEvent { get; private set; }
        public Event<CompletePaymentEvent> CompletePaymentEvent { get; private set; }
        public Event<CompleteCartEvent> CompleteCartEvent { get; private set; }
        public Event<CompensateTransactionEvent> CompensateTransactionEvent { get; private set; }
    }
}