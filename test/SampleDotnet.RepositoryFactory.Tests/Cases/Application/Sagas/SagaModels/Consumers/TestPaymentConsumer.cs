﻿using SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels;
using SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.Consumers;
using SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.DbContexts;
using SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.Entities;
using SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.Enums;
using SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.EventMessages;
using SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.Events;
using SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.Interfaces;
using SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.Services;
namespace SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.Consumers;

public class TestPaymentConsumer :
    IConsumer<StartPaymentEvent>,
    IConsumer<RollbackPaymentEvent>
{
    private readonly ITestPaymentService _paymentService;
    private readonly ILogger<TestPaymentConsumer> _logger;
    private readonly IPublishEndpoint _publishEndpoint;

    public TestPaymentConsumer(ITestPaymentService paymentService, IPublishEndpoint publishEndpoint, ILogger<TestPaymentConsumer> logger)
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
            await _publishEndpoint.Publish(new CompensateTransactionEvent(context.Message.CorrelationId));
        }
    }

    public async Task Consume(ConsumeContext<RollbackPaymentEvent> context)
    {
        _logger.LogInformation($"Received RollbackPayment: {context.Message.CorrelationId}");
        await _paymentService.RollbackPayment(context.Message.CorrelationId);
    }
}