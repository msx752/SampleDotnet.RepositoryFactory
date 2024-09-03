namespace SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels
{
    public class TransactionStateMachine : MassTransitStateMachine<TransactionState>
    {
        public TransactionStateMachine()
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
                        context.Instance.TransactionId = context.Data.CorrelationId;
                        context.Instance.PaymentAmount = context.Data.PaymentAmount;
                        context.Instance.CartItems = context.Data.CartItems;
                        context.Instance.Timestamp = DateTime.UtcNow;
                        context.Publish(new StartPaymentEvent(context.Data.CorrelationId, context.Data.PaymentAmount));
                    })
                    .TransitionTo(Processing));

            During(Processing,
                When(CompletePaymentEvent)
                    .Then(context =>
                    {
                        context.Publish(new StartCartEvent(context.Data.CorrelationId, context.Instance.CartItems));
                    }),
                When(CompleteCartEvent)
                    .Then(context =>
                    {
                        context.Instance.CurrentState = "Completed";
                    })
                    .TransitionTo(Completed),
                When(CompensateTransactionEvent)
                    .ThenAsync(async context =>
                    {
                        await context.Publish(new RollbackPaymentEvent(context.Data.CorrelationId));
                        await context.Publish(new RollbackCartEvent(context.Data.CorrelationId));
                    })
                    .TransitionTo(Rolledback));

            During(Completed,
                When(CompensateTransactionEvent)
                    .ThenAsync(async context =>
                    {
                        await context.Publish(new RollbackPaymentEvent(context.Data.CorrelationId));
                        await context.Publish(new RollbackCartEvent(context.Data.CorrelationId));
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