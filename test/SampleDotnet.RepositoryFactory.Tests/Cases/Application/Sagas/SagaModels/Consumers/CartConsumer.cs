namespace SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.Consumers;

public class CartConsumer :
IConsumer<StartCartEvent>,
IConsumer<RollbackCartEvent>
{
    private readonly ICartService _cartService;
    private readonly ILogger<CartConsumer> _logger;
    private readonly IPublishEndpoint _publishEndpoint;

    public CartConsumer(ICartService cartService, IPublishEndpoint publishEndpoint, ILogger<CartConsumer> logger)
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
            List<CartItemEntity> cartItems = context.Message.Items
                .Select(item => new CartItemEntity
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
            await _publishEndpoint.Publish(new RollbackCartEvent(context.Message.CorrelationId));
        }
    }

    public async Task Consume(ConsumeContext<RollbackCartEvent> context)
    {
        _logger.LogInformation($"Received RollbackCart: {context.Message.CorrelationId}");
        await _cartService.RollbackCart(context.Message.CorrelationId);
    }
}