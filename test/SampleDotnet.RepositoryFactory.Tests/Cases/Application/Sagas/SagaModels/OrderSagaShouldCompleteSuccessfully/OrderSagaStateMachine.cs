namespace SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.OrderSagaShouldCompleteSuccessfully;

public enum InventoryStatus
{
    Reserved,
    Cancelled
}

public enum OrderStatus
{
    Placed,
    Cancelled,
    Completed
}

public enum PaymentStatus
{
    Processed,
    Cancelled
}

public interface IInventoryService
{
    Task<bool> ReserveInventoryAsync(Guid correlationId, List<OrderItem> items);

    Task<bool> RollbackInventoryAsync(Guid correlationId);
}

public interface IOrderService
{
    Task<bool> CancelOrderAsync(Guid correlationId);
    Task<bool> ConfirmOrderAsync(Guid correlationId);
    Task<bool> PlaceOrderAsync(Guid correlationId, decimal amount, List<OrderItem> items);
}

public interface IPaymentService
{
    Task<bool> ProcessPaymentAsync(Guid correlationId, decimal amount);

    Task<bool> RollbackPaymentAsync(Guid correlationId);
}

public class InventoryConsumer : IConsumer<StartInventoryEvent>
{
    private readonly IInventoryService _inventoryService;

    public InventoryConsumer(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    public async Task Consume(ConsumeContext<StartInventoryEvent> context)
    {
        var success = await _inventoryService.ReserveInventoryAsync(context.Message.CorrelationId, context.Message.Items);
        if (success)
        {
            await context.Publish(new CompleteInventoryEvent(context.Message.CorrelationId));
        }
        else
        {
            await context.Publish(new CompensateOrderEvent(context.Message.CorrelationId));
        }
    }
}

public class InventoryEntity
{
    public Guid Id { get; set; }
    public Guid CorrelationId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public InventoryStatus Status { get; set; }
}

public class InventoryService : IInventoryService
{
    private readonly IUnitOfWork _unitOfWork;

    public InventoryService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public virtual async Task<bool> ReserveInventoryAsync(Guid correlationId, List<OrderItem> items)
    {
        using var repo = _unitOfWork.CreateRepository<InventoryDbContext>();

        // Simulate reserving inventory for each item
        foreach (var item in items)
        {
            repo.Add(new InventoryEntity
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                CorrelationId = correlationId,
                Status = InventoryStatus.Reserved
            });
        }

        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RollbackInventoryAsync(Guid correlationId)
    {
        using var repo = _unitOfWork.CreateRepository<InventoryDbContext>();

        var reservedItems = await repo.Where<InventoryEntity>(i => i.CorrelationId == correlationId && i.Status == InventoryStatus.Reserved).ToListAsync();
        foreach (var item in reservedItems)
        {
            item.Status = InventoryStatus.Cancelled;
        }

        await _unitOfWork.SaveChangesAsync();
        return true;
    }
}

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options)
        : base(options)
    {
    }

    public DbSet<OrderEntity> Orders { get; set; }
}

public class OrderDetails
{
    public decimal Amount { get; set; }
    public List<OrderItem> Items { get; set; }
}

public class OrderEntity
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public Guid CorrelationId { get; set; }
    public List<OrderItem> Items { get; set; }
    public OrderStatus Status { get; set; }
}

public class OrderSagaState : SagaStateMachineInstance, ISagaVersion
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; }
    public OrderDetails OrderDetails { get; set; }
    public bool PaymentCompleted { get; set; }
    public int Version { get; set; }
}

public class OrderSagaStateMachine : MassTransitStateMachine<OrderSagaState>
{
    public OrderSagaStateMachine()
    {
        InstanceState(x => x.CurrentState);

        Event(() => StartOrder, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => CompletePayment, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => CompleteInventory, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => CompensateOrder, x => x.CorrelateById(m => m.Message.CorrelationId));

        Initially(
            When(StartOrder)
                .Then(context =>
                {
                    context.Instance.OrderDetails = new OrderDetails
                    {
                        Amount = context.Data.Amount,
                        Items = context.Data.Items
                    };
                })
                .TransitionTo(Processing)
                .Publish(context => new StartPaymentEvent(context.Data.CorrelationId, context.Data.Amount))
        );

        During(Processing,
            When(CompletePayment)
                .Then(context =>
                {
                    context.Instance.PaymentCompleted = true;
                })
                .Publish(context => new StartInventoryEvent(context.Data.CorrelationId, context.Instance.OrderDetails.Items))
                .TransitionTo(InventoryProcessing),

            When(CompensateOrder)
                .ThenAsync(async context =>
                {
                    await context.Publish(new RollbackPaymentEvent(context.Data.CorrelationId));
                    await context.Publish(new RollbackOrderEvent(context.Data.CorrelationId));
                    await context.Publish(new RollbackInventoryEvent(context.Data.CorrelationId));
                })
                .TransitionTo(Compensated)
        );

        During(InventoryProcessing,
            When(CompleteInventory)
                .ThenAsync(async context =>
                {
                    await context.Publish(new ConfirmOrderEvent(context.Data.CorrelationId));
                })
                .TransitionTo(Completed),

            When(CompensateOrder)
                .ThenAsync(async context =>
                {
                    await context.Publish(new RollbackPaymentEvent(context.Data.CorrelationId));
                    await context.Publish(new RollbackOrderEvent(context.Data.CorrelationId));
                    await context.Publish(new RollbackInventoryEvent(context.Data.CorrelationId));
                })
                .TransitionTo(Compensated)
        );

        SetCompletedWhenFinalized();
    }

    public State Compensated { get; private set; }
    public Event<CompensateOrderEvent> CompensateOrder { get; private set; }
    public State Completed { get; private set; }
    public Event<CompleteInventoryEvent> CompleteInventory { get; private set; }
    public Event<CompletePaymentEvent> CompletePayment { get; private set; }
    public State InventoryProcessing { get; private set; }
    public State Processing { get; private set; }
    public Event<StartOrderEvent> StartOrder { get; private set; }
}

public class OrderService : IOrderService
{
    private readonly IUnitOfWork _unitOfWork;

    public OrderService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> CancelOrderAsync(Guid correlationId)
    {
        using var repo = _unitOfWork.CreateRepository<OrderDbContext>();
        var order = await repo.FirstOrDefaultAsync<OrderEntity>(o => o.CorrelationId == correlationId);
        if (order != null)
        {
            order.Status = OrderStatus.Cancelled;
            await _unitOfWork.SaveChangesAsync();
        }

        return true;
    }

    public async Task<bool> ConfirmOrderAsync(Guid correlationId)
    {
        using var repo = _unitOfWork.CreateRepository<OrderDbContext>();
        var order = await repo.FirstOrDefaultAsync<OrderEntity>(o => o.CorrelationId == correlationId);
        if (order != null)
        {
            order.Status = OrderStatus.Placed;
            await _unitOfWork.SaveChangesAsync();
        }

        return true;
    }

    public async Task<bool> PlaceOrderAsync(Guid correlationId, decimal amount, List<OrderItem> items)
    {
        using var repo = _unitOfWork.CreateRepository<OrderDbContext>();
        // Add order to the repository
        repo.Add(new OrderEntity
        {
            CorrelationId = correlationId,
            Amount = amount,
            Items = items,
            Status = OrderStatus.Placed
        });

        await _unitOfWork.SaveChangesAsync();

        // Publish StartOrderEvent to initiate the saga
        // await _publishEndpoint.Publish(new StartOrderEvent(correlationId, amount, items));

        return true;
    }
}
public class PaymentConsumer : IConsumer<StartPaymentEvent>
{
    private readonly IPaymentService _paymentService;

    public PaymentConsumer(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    public async Task Consume(ConsumeContext<StartPaymentEvent> context)
    {
        var success = await _paymentService.ProcessPaymentAsync(context.Message.CorrelationId, context.Message.Amount);
        if (success)
        {
            await context.Publish(new CompletePaymentEvent(context.Message.CorrelationId));
        }
        else
        {
            await context.Publish(new CompensateOrderEvent(context.Message.CorrelationId));
        }
    }
}

public class PaymentDbContext : DbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options)
        : base(options)
    {
    }

    public DbSet<PaymentEntity> Payments { get; set; }
}

public class PaymentEntity
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; }
    public Guid TransactionId { get; set; }
}

public class PaymentService : IPaymentService
{
    private readonly IUnitOfWork _unitOfWork;

    public PaymentService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> ProcessPaymentAsync(Guid correlationId, decimal amount)
    {
        using var repo = _unitOfWork.CreateRepository<PaymentDbContext>();

        // Simulate payment processing
        repo.Add(new PaymentEntity
        {
            TransactionId = correlationId,
            Amount = amount,
            Status = PaymentStatus.Processed
        });

        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RollbackPaymentAsync(Guid correlationId)
    {
        using var repo = _unitOfWork.CreateRepository<PaymentDbContext>();
        var payment = await repo.FirstOrDefaultAsync<PaymentEntity>(p => p.TransactionId == correlationId && p.Status == PaymentStatus.Processed);
        if (payment != null)
        {
            payment.Status = PaymentStatus.Cancelled;
            await _unitOfWork.SaveChangesAsync();
        }

        return true;
    }
}
public record StartOrderEvent(Guid CorrelationId, decimal Amount, List<OrderItem> Items) : CorrelatedBy<Guid>;
public record CompletePaymentEvent(Guid CorrelationId) : CorrelatedBy<Guid>;
public record CompleteInventoryEvent(Guid CorrelationId) : CorrelatedBy<Guid>;
public record CompensateOrderEvent(Guid CorrelationId) : CorrelatedBy<Guid>;
public record StartPaymentEvent(Guid CorrelationId, decimal Amount) : CorrelatedBy<Guid>;
public record StartInventoryEvent(Guid CorrelationId, List<OrderItem> Items) : CorrelatedBy<Guid>;
public record ConfirmOrderEvent(Guid CorrelationId) : CorrelatedBy<Guid>;
public record RollbackPaymentEvent(Guid CorrelationId) : CorrelatedBy<Guid>;
public record RollbackInventoryEvent(Guid CorrelationId) : CorrelatedBy<Guid>;
public record RollbackOrderEvent(Guid CorrelationId) : CorrelatedBy<Guid>;

public class RollbackInventoryConsumer : IConsumer<RollbackInventoryEvent>
{
    private readonly IInventoryService _inventoryService;

    public RollbackInventoryConsumer(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    public async Task Consume(ConsumeContext<RollbackInventoryEvent> context)
    {
        await _inventoryService.RollbackInventoryAsync(context.Message.CorrelationId);
    }
}

public class RollbackPaymentConsumer : IConsumer<RollbackPaymentEvent>
{
    private readonly IPaymentService _paymentService;

    public RollbackPaymentConsumer(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    public async Task Consume(ConsumeContext<RollbackPaymentEvent> context)
    {
        await _paymentService.RollbackPaymentAsync(context.Message.CorrelationId);
    }
}

public class OrderItem
{
    public Guid Id { get; set; }
    public decimal Price { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}
public class InventoryDbContext : DbContext
{
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options)
        : base(options)
    {
    }

    public DbSet<InventoryEntity> InventoryItems { get; set; }
}
public class OrderConsumer : IConsumer<StartOrderEvent>
{
    private readonly IOrderService _orderService;

    public OrderConsumer(IOrderService orderService)
    {
        _orderService = orderService;
    }

    public async Task Consume(ConsumeContext<StartOrderEvent> context)
    {
        var success = await _orderService.PlaceOrderAsync(context.Message.CorrelationId, context.Message.Amount, context.Message.Items);

        if (!success)
        {
            // If the order cannot be placed, trigger compensation
            await context.Publish(new CompensateOrderEvent(context.Message.CorrelationId));
        }
    }
}
public class ConfirmOrderConsumer : IConsumer<ConfirmOrderEvent>
{
    private readonly IOrderService _orderService;

    public ConfirmOrderConsumer(IOrderService orderService)
    {
        _orderService = orderService;
    }

    public async Task Consume(ConsumeContext<ConfirmOrderEvent> context)
    {
        var success = await _orderService.ConfirmOrderAsync(context.Message.CorrelationId);

        if (success)
        {
            // Log or take further actions to notify that the order has been confirmed
            Console.WriteLine($"Order {context.Message.CorrelationId} confirmed.");
        }
        else
        {
            // In case confirmation fails, you might want to handle this case
            Console.WriteLine($"Order confirmation failed for {context.Message.CorrelationId}.");
        }
    }
}

public class RollbackOrderConsumer : IConsumer<RollbackOrderEvent>
{
    private readonly IOrderService _orderService;

    public RollbackOrderConsumer(IOrderService orderService)
    {
        _orderService = orderService;
    }

    public async Task Consume(ConsumeContext<RollbackOrderEvent> context)
    {
        var success = await _orderService.CancelOrderAsync(context.Message.CorrelationId);
    }
}
