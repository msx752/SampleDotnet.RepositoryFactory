using MassTransit;
using Microsoft.Extensions.Logging;

namespace SampleDotnet.RepositoryFactory.Tests.TestModels.Sagas
{
    public class CartHandler :
    IConsumer<StartCart>,
    IConsumer<RollbackCart>
    {
        private readonly ICartService _cartService;
        private readonly ILogger<CartHandler> _logger;
        private readonly IPublishEndpoint _publishEndpoint;

        public CartHandler(ICartService cartService, IPublishEndpoint publishEndpoint, ILogger<CartHandler> logger)
        {
            _cartService = cartService;
            _publishEndpoint = publishEndpoint;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<StartCart> context)
        {
            _logger.LogInformation($"Received StartCart: {context.Message.CorrelationId}");

            try
            {
                List<CartItem> cartItems = context.Message.Items
                    .Select(item => new CartItem
                    {
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        Price = item.Price
                    })
                    .ToList();
                await _cartService.ProcessCart(context.Message.CorrelationId, cartItems);
                await _publishEndpoint.Publish(new CompleteCart(context.Message.CorrelationId));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to process cart: {ex.Message}");
                await _publishEndpoint.Publish(new RollbackCart(context.Message.CorrelationId));
            }
        }

        public async Task Consume(ConsumeContext<RollbackCart> context)
        {
            _logger.LogInformation($"Received RollbackCart: {context.Message.CorrelationId}");
            await _cartService.RollbackCart(context.Message.CorrelationId);
        }
    }

}
