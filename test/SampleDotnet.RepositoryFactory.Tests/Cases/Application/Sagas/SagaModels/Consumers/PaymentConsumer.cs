﻿namespace SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.Consumers;

public class PaymentConsumer :
    IConsumer<StartPaymentEvent>,
    IConsumer<RollbackPaymentEvent>
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentConsumer> _logger;
    private readonly IPublishEndpoint _publishEndpoint;

    public PaymentConsumer(IPaymentService paymentService, IPublishEndpoint publishEndpoint, ILogger<PaymentConsumer> logger)
    {
        _paymentService = paymentService;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<StartPaymentEvent> context)
    {
        _logger.LogInformation($"Received StartPayment: {context.Message.CorrelationId}");

        try
        {
            await _paymentService.ProcessPayment(context.Message.CorrelationId, context.Message.Amount);
            await _publishEndpoint.Publish(new CompletePaymentEvent(context.Message.CorrelationId));
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to process payment: {ex.Message}");
            await _publishEndpoint.Publish(new RollbackPaymentEvent(context.Message.CorrelationId));
        }
    }

    public async Task Consume(ConsumeContext<RollbackPaymentEvent> context)
    {
        _logger.LogInformation($"Received RollbackPayment: {context.Message.CorrelationId}");
        await _paymentService.RollbackPayment(context.Message.CorrelationId);
    }
}