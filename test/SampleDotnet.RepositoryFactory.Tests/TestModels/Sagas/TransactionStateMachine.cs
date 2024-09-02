using MassTransit;

namespace SampleDotnet.RepositoryFactory.Tests.TestModels.Sagas
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
                        context.Publish(new StartPayment(context.Data.CorrelationId, context.Data.PaymentAmount));
                    })
                    .TransitionTo(Processing));

            During(Processing,
                When(CompletePaymentEvent)
                    .Then(context =>
                    {
                        context.Publish(new StartCart(context.Data.CorrelationId, context.Instance.CartItems));
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
                        await context.Publish(new RollbackPayment(context.Data.CorrelationId));
                        await context.Publish(new RollbackCart(context.Data.CorrelationId));
                    })
                    .TransitionTo(Rolledback));

            During(Completed,
                When(CompensateTransactionEvent)
                    .ThenAsync(async context =>
                    {
                        await context.Publish(new RollbackPayment(context.Data.CorrelationId));
                        await context.Publish(new RollbackCart(context.Data.CorrelationId));
                    })
                    .TransitionTo(Rolledback));

            SetCompletedWhenFinalized();
        }

        public State Processing { get; private set; }
        public State Completed { get; private set; }
        public State Rolledback { get; private set; }

        public Event<StartTransaction> StartTransactionEvent { get; private set; }
        public Event<CompletePayment> CompletePaymentEvent { get; private set; }
        public Event<CompleteCart> CompleteCartEvent { get; private set; }
        public Event<CompensateTransaction> CompensateTransactionEvent { get; private set; }
    }

}
