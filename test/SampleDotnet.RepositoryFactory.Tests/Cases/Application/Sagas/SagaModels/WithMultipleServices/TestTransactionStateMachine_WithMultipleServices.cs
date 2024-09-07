namespace SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.WithMultipleServices;

public enum CartStatus
{
    Reserved,  // Indicates that the cart items have been reserved
    Released   // Indicates that the cart items have been released
}

public enum InventoryStatus
{
    Reserved,
    Released
}

public enum PaymentStatus
{
    Pending,    // Payment has been initiated but not yet processed
    Processed,  // Payment has been successfully processed
    Failed,     // Payment processing failed
    Cancelled   // Payment has been rolled back or cancelled
}

public interface ITestCartService
{
    Task<bool> ReleaseCartAsync(Guid correlationId);

    Task<bool> ReserveCartAsync(Guid correlationId, List<SagaCartItem> items);
}

public interface ITestInventoryService
{
    Task<bool> ReleaseInventoryAsync(Guid correlationId);

    Task<bool> ReserveInventoryAsync(Guid correlationId, List<SagaCartItem> items);
}

public interface ITestPaymentService
{
    Task<List<SagaCartItem>> GetItemsForTransactionAsync(Guid correlationId);

    Task<bool> ProcessPaymentAsync(Guid correlationId, decimal amount);

    Task<bool> RollbackPaymentAsync(Guid correlationId);
}

public class CompensateTransactionEvent
{
    public CompensateTransactionEvent(Guid correlationId)
    {
        CorrelationId = correlationId;
    }

    public Guid CorrelationId { get; set; }
}

public class CompleteCartEvent
{
    public CompleteCartEvent(Guid correlationId, List<SagaCartItem> items)
    {
        CorrelationId = correlationId;
        Items = items;
    }

    public Guid CorrelationId { get; set; }
    public List<SagaCartItem> Items { get; }
}

public class CompleteInventoryEvent
{
    public CompleteInventoryEvent(Guid correlationId)
    {
        CorrelationId = correlationId;
    }

    public Guid CorrelationId { get; set; }
}

public class CompletePaymentEvent
{
    public Guid CorrelationId { get; set; }  // Must match constructor parameter name and type
    public List<SagaCartItem> Items { get; set; }  // Must match constructor parameter name and type

    public CompletePaymentEvent(Guid correlationId, List<SagaCartItem> items)
    {
        CorrelationId = correlationId;
        Items = items;
    }
}

public class FinalizeTransactionEvent
{
    public FinalizeTransactionEvent(Guid correlationId)
    {
        CorrelationId = correlationId;
    }

    public Guid CorrelationId { get; set; }
}

public class RollbackCartEvent
{
    public RollbackCartEvent(Guid correlationId)
    {
        CorrelationId = correlationId;
    }

    public Guid CorrelationId { get; set; }
}

public class RollbackInventoryEvent
{
    public RollbackInventoryEvent(Guid correlationId)
    {
        CorrelationId = correlationId;
    }

    public Guid CorrelationId { get; set; }
}

public class RollbackPaymentEvent
{
    public RollbackPaymentEvent(Guid correlationId)
    {
        CorrelationId = correlationId;
    }

    public Guid CorrelationId { get; set; }
}

public class SagaCartItem
{
    public decimal Price { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}

public record StartCartEvent(Guid CorrelationId, List<SagaCartItem> Items) : CorrelatedBy<Guid>;

public class StartInventoryEvent
{
    public StartInventoryEvent(Guid correlationId, List<SagaCartItem> items)
    {
        CorrelationId = correlationId;
        Items = items;
    }

    public Guid CorrelationId { get; set; }
    public List<SagaCartItem> Items { get; set; }
}

public class StartPaymentEvent
{
    public StartPaymentEvent(Guid correlationId, decimal amount)
    {
        CorrelationId = correlationId;
        Amount = amount;
    }

    public decimal Amount { get; set; }
    public Guid CorrelationId { get; set; }  // Unique identifier for the transaction or saga instance
                                             // The total amount to be processed in the payment
}

public class StartTransactionEvent
{
    public StartTransactionEvent(Guid correlationId, decimal totalAmount, List<SagaCartItem> items)
    {
        CorrelationId = correlationId;
        TotalAmount = totalAmount;
        Items = items;
    }

    public Guid CorrelationId { get; set; }
    public List<SagaCartItem> Items { get; set; }
    public decimal TotalAmount { get; set; }
}

public class TestCartDbContext : DbContext
{
    public TestCartDbContext(DbContextOptions<TestCartDbContext> options)
        : base(options)
    {
    }

    public DbSet<TestCartEntity> CartItems { get; set; }  // DbSet representing the collection of cart items
}

public class TestCartEntity
{
    public Guid CorrelationId { get; set; }
    public Guid Id { get; set; }  // Unique identifier for the cart item
    public Guid ProductId { get; set; }  // Identifier for the product associated with this cart item
    public int Quantity { get; set; }  // Quantity of the product in the cart

    // Correlation ID linking the cart item to a specific transaction or saga instance
    public CartStatus Status { get; set; }  // Status of the cart item (Reserved or Released)
}

public class TestCartService_Success : ITestCartService
{
    private readonly IUnitOfWork _unitOfWork;

    public TestCartService_Success(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> ReleaseCartAsync(Guid correlationId)
    {
        // Simulate releasing the cart
        using var repo = _unitOfWork.CreateRepository<TestCartDbContext>();
        var reservedItems = repo.Where<TestCartEntity>(c => c.CorrelationId == correlationId && c.Status == CartStatus.Reserved).ToList();
        foreach (var item in reservedItems)
        {
            item.Status = CartStatus.Released;
        }

        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ReserveCartAsync(Guid correlationId, List<SagaCartItem> items)
    {
        using var repo = _unitOfWork.CreateRepository<TestCartDbContext>();
        // Simulate reserving items in the cart
        foreach (var item in items)
        {
            repo.Insert<TestCartEntity>(new TestCartEntity
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                CorrelationId = correlationId,
                Status = CartStatus.Reserved
            });
        }

        await _unitOfWork.SaveChangesAsync();
        return true;
    }
}

public class TestInventoryConsumer : IConsumer<StartInventoryEvent>
{
    private readonly ITestInventoryService _inventoryService;

    public TestInventoryConsumer(ITestInventoryService inventoryService)
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
            await context.Publish(new CompensateTransactionEvent(context.Message.CorrelationId));
        }
    }
}

public class TestInventoryDbContext : DbContext
{
    public TestInventoryDbContext(DbContextOptions<TestInventoryDbContext> options)
        : base(options)
    {
    }

    public DbSet<TestInventoryEntity> InventoryItems { get; set; }
}

public class TestInventoryEntity
{
    public Guid CorrelationId { get; set; }
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public InventoryStatus Status { get; set; }
}

public class TestInventoryService_Success : ITestInventoryService
{
    private readonly IUnitOfWork _unitOfWork;

    public TestInventoryService_Success(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> ReleaseInventoryAsync(Guid correlationId)
    {
        // Simulate releasing inventory
        using var repo = _unitOfWork.CreateRepository<TestInventoryDbContext>();
        var reservedItems = repo.Where<TestInventoryEntity>(i => i.CorrelationId == correlationId && i.Status == InventoryStatus.Reserved).ToList();
        foreach (var item in reservedItems)
        {
            item.Status = InventoryStatus.Released;
        }

        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ReserveInventoryAsync(Guid correlationId, List<SagaCartItem> items)
    {
        // Simulate reserving inventory
        using var repo = _unitOfWork.CreateRepository<TestInventoryDbContext>();
        foreach (var item in items)
        {
            repo.Add(new TestInventoryEntity
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
}

public class TestPaymentDbContext : DbContext
{
    public TestPaymentDbContext(DbContextOptions<TestPaymentDbContext> options)
        : base(options)
    {
    }

    public DbSet<TestPaymentEntity> Payments { get; set; }  // DbSet representing the collection of payment transactions
}

public class TestPaymentService : ITestPaymentService
{
    private readonly IUnitOfWork _unitOfWork;

    public TestPaymentService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> ProcessPaymentAsync(Guid correlationId, decimal amount)
    {
        // Simulate processing payment
        using var repo = _unitOfWork.CreateRepository<TestPaymentDbContext>();
        repo.Add(new TestPaymentEntity
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
        // Simulate rolling back payment
        using var repo = _unitOfWork.CreateRepository<TestPaymentDbContext>();
        var payment = repo.FirstOrDefault<TestPaymentEntity>(p => p.TransactionId == correlationId && p.Status == PaymentStatus.Processed);
        if (payment != null)
        {
            payment.Status = PaymentStatus.Cancelled;
            await _unitOfWork.SaveChangesAsync();
        }

        return true;
    }

    public async Task<List<SagaCartItem>> GetItemsForTransactionAsync(Guid correlationId)
    {
        // Example method to simulate fetching items for a transaction from the database
        // In practice, you would adjust this logic based on your application's data structure

        using var repo = _unitOfWork.CreateRepository<TestPaymentDbContext>();
        var items = await repo
            .Where<TestPaymentEntity>(p => p.TransactionId == correlationId)
            .Select(p => new SagaCartItem
            {
                ProductId = p.Id,  // Example mapping, adjust based on your actual data
                Quantity = 1,      // Example fixed quantity, adjust as needed
                Price = p.Amount   // Example using the payment amount, adjust as needed
            }).ToListAsync();

        return items;
    }
}

public class TestTransactionState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; }
    public List<SagaCartItem> Items { get; set; } // Ensure this property exists if needed by StartInventoryEvent
}

public class TestTransactionStateMachine_WithMultipleServices : MassTransitStateMachine<TestTransactionState>
{
    public TestTransactionStateMachine_WithMultipleServices()
    {
        InstanceState(x => x.CurrentState);

        Event(() => StartTransaction, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => CompletePayment, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => CompleteCart, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => CompleteInventory, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => FinalizeTransaction, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => CompensateTransaction, x => x.CorrelateById(m => m.Message.CorrelationId));

        Initially(
            When(StartTransaction)
                .TransitionTo(Processing)
                .Publish(context => new StartPaymentEvent(context.Saga.CorrelationId, context.Message.TotalAmount))
        );

        During(Processing,
            When(CompletePayment)
                .Publish(context => new StartCartEvent(context.Saga.CorrelationId, context.Message.Items)),
            When(CompleteCart)
                .Publish(context => new StartInventoryEvent(context.Saga.CorrelationId, context.Message.Items)),
            When(CompleteInventory)
                .Publish(context => new FinalizeTransactionEvent(context.Saga.CorrelationId)),
            When(FinalizeTransaction)
                .Then(context =>
                {
                    context.Saga.CurrentState = "Completed";
                })
                .TransitionTo(Completed),
            When(CompensateTransaction)
                .TransitionTo(Rolledback)
                .Publish(context => new RollbackPaymentEvent(context.Saga.CorrelationId))
                .Publish(context => new RollbackCartEvent(context.Saga.CorrelationId))
                .Publish(context => new RollbackInventoryEvent(context.Saga.CorrelationId))
        );

        SetCompletedWhenFinalized();
    }

    public Event<CompensateTransactionEvent> CompensateTransaction { get; private set; }
    public Event<CompleteCartEvent> CompleteCart { get; private set; }
    public State Completed { get; private set; }
    public Event<CompleteInventoryEvent> CompleteInventory { get; private set; }
    public Event<CompletePaymentEvent> CompletePayment { get; private set; }
    public Event<FinalizeTransactionEvent> FinalizeTransaction { get; private set; }
    public State Processing { get; private set; }
    public State Rolledback { get; private set; }

    public Event<StartTransactionEvent> StartTransaction { get; private set; }
}

public class TestPaymentEntity
{
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid Id { get; set; }  // Unique identifier for the payment transaction

    // The total amount processed in the payment
    public PaymentStatus Status { get; set; }

    public Guid TransactionId { get; set; }  // Correlation ID linking the payment to a specific transaction or saga instance
                                             // Status of the payment (Pending, Processed, Failed, Cancelled)
                                             // The timestamp when the payment was created
}

public class TestCartConsumer : IConsumer<StartCartEvent>
{
    private readonly ITestCartService _cartService;

    public TestCartConsumer(ITestCartService cartService)
    {
        _cartService = cartService;
    }

    public async Task Consume(ConsumeContext<StartCartEvent> context)
    {
        var correlationId = context.Message.CorrelationId;

        // Simulate reserving items in the cart
        bool success = await _cartService.ReserveCartAsync(correlationId, context.Message.Items);

        if (success)
        {
            // If the cart reservation is successful, publish CompleteCartEvent
            await context.Publish(new CompleteCartEvent(correlationId, context.Message.Items));
        }
        else
        {
            // If the cart reservation fails, publish CompensateTransactionEvent to trigger compensation
            await context.Publish(new CompensateTransactionEvent(correlationId));
        }
    }
}

public class TestPaymentConsumer : IConsumer<StartPaymentEvent>
{
    private readonly ITestPaymentService _paymentService;

    public TestPaymentConsumer(ITestPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    public async Task Consume(ConsumeContext<StartPaymentEvent> context)
    {
        var correlationId = context.Message.CorrelationId;
        var amount = context.Message.Amount;

        // Simulate processing the payment
        bool success = await _paymentService.ProcessPaymentAsync(correlationId, amount);

        if (success)
        {
            // Assume that `Items` need to be passed to the next event.
            // If the `Items` property is required for subsequent events,
            // we should fetch it from somewhere in the consumer or service.
            var items = await _paymentService.GetItemsForTransactionAsync(correlationId);

            // If the payment is successful, publish CompletePaymentEvent with Items
            await context.Publish(new CompletePaymentEvent(correlationId, items));
        }
        else
        {
            // If the payment fails, publish CompensateTransactionEvent to trigger compensation
            await context.Publish(new CompensateTransactionEvent(correlationId));
        }
    }
}