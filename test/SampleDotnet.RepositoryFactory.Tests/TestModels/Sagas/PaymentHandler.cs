using MassTransit;
using Microsoft.Extensions.Logging;

namespace SampleDotnet.RepositoryFactory.Tests.TestModels.Sagas
{
    public class PaymentHandler :
        IConsumer<StartPayment>,
        IConsumer<RollbackPayment>
    {
        private readonly IPaymentService _paymentService;
        private readonly ILogger<PaymentHandler> _logger;
        private readonly IPublishEndpoint _publishEndpoint;

        public PaymentHandler(IPaymentService paymentService, IPublishEndpoint publishEndpoint, ILogger<PaymentHandler> logger)
        {
            _paymentService = paymentService;
            _publishEndpoint = publishEndpoint;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<StartPayment> context)
        {
            _logger.LogInformation($"Received StartPayment: {context.Message.CorrelationId}");

            try
            {
                await _paymentService.ProcessPayment(context.Message.CorrelationId, context.Message.Amount);
                await _publishEndpoint.Publish(new CompletePayment(context.Message.CorrelationId));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to process payment: {ex.Message}");
                await _publishEndpoint.Publish(new RollbackPayment(context.Message.CorrelationId));
            }
        }

        public async Task Consume(ConsumeContext<RollbackPayment> context)
        {
            _logger.LogInformation($"Received RollbackPayment: {context.Message.CorrelationId}");
            await _paymentService.RollbackPayment(context.Message.CorrelationId);
        }
    }

}
