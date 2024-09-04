namespace SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.Consumers;

public class TestCartConsumer :
IConsumer<StartCartEvent>,
IConsumer<RollbackCartEvent>
{
    private readonly ITestCartService _cartService;
    private readonly ILogger<TestCartConsumer> _logger;
    private readonly IPublishEndpoint _publishEndpoint;

    public TestCartConsumer(ITestCartService cartService, IPublishEndpoint publishEndpoint, ILogger<TestCartConsumer> logger)
    {
        _cartService = cartService;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<StartCartEvent> context)
    {
        _logger.LogInformation($"Received StartCart: {context.Message.CorrelationId}");

        try
        {
            List<TestCartItemEntity> cartItems = context.Message.Items
                .Select(item => new TestCartItemEntity
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    Price = item.Price
                })
                .ToList();
            await _cartService.ProcessCart(context.Message.CorrelationId, cartItems);
            await _publishEndpoint.Publish(new CompleteCartEvent(context.Message.CorrelationId));
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to process cart: {ex.Message}");
            await _publishEndpoint.Publish(new CompensateTransactionEvent(context.Message.CorrelationId));
        }
    }

    public async Task Consume(ConsumeContext<RollbackCartEvent> context)
    {
        _logger.LogInformation($"Received RollbackCart: {context.Message.CorrelationId}");
        await _cartService.RollbackCart(context.Message.CorrelationId);
    }
}