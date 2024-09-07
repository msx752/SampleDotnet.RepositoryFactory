using DotNet.Testcontainers.Configurations;
using Moq;
using SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.OrderSagaShouldCompleteSuccessfully;

namespace SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.Tests
{
    [Collection("Shared Collection")]
    public class OrderSagaTests
    {
        // A container for running SQL Server in Docker for testing purposes.
        private readonly SharedContainerFixture _shared;

        // Constructor initializes the SQL container with specific configurations.
        public OrderSagaTests(SharedContainerFixture fixture)
        {
            _shared = fixture;
        }

        [Fact]
        public async Task OrderSaga_ShouldCompleteSuccessfully()
        {
            // Arrange: Set up an IHostBuilder to configure services with DbContexts and messaging.
            IHostBuilder host = Host.CreateDefaultBuilder().ConfigureServices((services) =>
            {
                // Configure DbContexts for Order, Payment, and Inventory using test SQL connections.
                services.AddDbContextFactory<OrderDbContext>(options =>
                    options.UseTestSqlConnection(_shared, "OrderSaga_ShouldCompleteSuccessfully_OrderDb"));

                services.AddDbContextFactory<PaymentDbContext>(options =>
                    options.UseTestSqlConnection(_shared, "OrderSaga_ShouldCompleteSuccessfully_PaymentDb"));

                services.AddDbContextFactory<InventoryDbContext>(options =>
                    options.UseTestSqlConnection(_shared, "OrderSaga_ShouldCompleteSuccessfully_InventoryDb"));

                // Register application services for order, payment, and inventory.
                services.AddTransient<IOrderService, OrderService>();
                services.AddTransient<IPaymentService, PaymentService>();
                services.AddTransient<IInventoryService, InventoryService>();

                // Register repository factory for Unit of Work pattern.
                services.AddRepositoryFactory(ServiceLifetime.Transient);

                // Configure MassTransit with a test harness using in-memory transport for message processing.
                services.AddMassTransitTestHarness(x =>
                {
                    // Register the consumers responsible for processing events.
                    x.AddConsumer<OrderConsumer>();
                    x.AddConsumer<ConfirmOrderConsumer>();
                    x.AddConsumer<PaymentConsumer>();
                    x.AddConsumer<InventoryConsumer>();

                    // Configure in-memory messaging.
                    x.UsingInMemory((context, cfg) =>
                    {
                        cfg.ConfigureEndpoints(context); // Automatically configure the endpoints for consumers.
                    });

                    // Add the saga state machine to handle the order saga, using an in-memory repository.
                    x.AddSagaStateMachine<OrderSagaStateMachine, OrderSagaState>()
                        .InMemoryRepository();
                });
            });

            // Act: Build the host and start the test harness.
            using (IHost build = host.Build())
            {
                // Ensure the in-memory test databases exist for Order, Payment, and Inventory.
                build.Services.EnsureDatabaseExists<OrderDbContext>();
                build.Services.EnsureDatabaseExists<PaymentDbContext>();
                build.Services.EnsureDatabaseExists<InventoryDbContext>();

                // Get the MassTransit test harness to simulate the message handling environment.
                var harness = build.Services.CreateScope().ServiceProvider.GetRequiredService<ITestHarness>();
                await harness.Start(); // Start the harness to begin consuming messages.

                // Get the saga harness to track the state and lifecycle of the order saga.
                var sagaHarness = harness.GetSagaStateMachineHarness<OrderSagaStateMachine, OrderSagaState>();

                // Generate a unique correlation ID for this saga instance.
                var correlationId = Guid.NewGuid();

                // Initiate the order saga by publishing the StartOrderEvent.
                await harness.Bus.Publish(new StartOrderEvent(correlationId, 100M, new List<OrderItem>
        {
            new() { ProductId = Guid.NewGuid(), Quantity = 1, Price = 100M }
        }));

                // Assert: Verify that the saga is created and has entered the processing state.
                Assert.True(await sagaHarness.Created.Any(x => x.CorrelationId == correlationId));

                // Assert: Verify that each stage of the saga lifecycle is processed successfully.
                Assert.True(await harness.Consumed.Any<StartOrderEvent>()); // Verify StartOrderEvent is consumed.
                Assert.True(await harness.Published.Any<StartPaymentEvent>()); // Verify StartPaymentEvent is published.

                Assert.True(await harness.Consumed.Any<StartPaymentEvent>()); // Verify StartPaymentEvent is consumed.
                Assert.True(await harness.Published.Any<CompletePaymentEvent>()); // Verify CompletePaymentEvent is published.

                Assert.True(await harness.Consumed.Any<CompletePaymentEvent>()); // Verify CompletePaymentEvent is consumed.
                Assert.True(await harness.Published.Any<StartInventoryEvent>()); // Verify StartInventoryEvent is published.

                Assert.True(await harness.Consumed.Any<StartInventoryEvent>()); // Verify StartInventoryEvent is consumed.
                Assert.True(await harness.Published.Any<CompleteInventoryEvent>()); // Verify CompleteInventoryEvent is published.

                Assert.True(await harness.Consumed.Any<CompleteInventoryEvent>()); // Verify CompleteInventoryEvent is consumed.
                Assert.True(await harness.Published.Any<ConfirmOrderEvent>()); // Verify ConfirmOrderEvent is published.

                Assert.True(await harness.Consumed.Any<ConfirmOrderEvent>()); // Verify ConfirmOrderEvent is consumed.

                // Assert: Verify that the saga has successfully transitioned to the Completed state.
                var instance = sagaHarness.Created.ContainsInState(correlationId, sagaHarness.StateMachine, sagaHarness.StateMachine.Completed);
                instance.ShouldNotBeNull(); // Ensure the saga reaches the Completed state.

                // Assert: Verify the final states of the Payment, Order, and Inventory entities in their respective databases.

                // Check the final state in PaymentDbContext to verify that payment is processed.
                using (var serviceScope = build.Services.CreateScope())
                using (var unitOfWork = serviceScope.ServiceProvider.GetRequiredService<IUnitOfWork>())
                using (var repo = unitOfWork.CreateRepository<PaymentDbContext>())
                {
                    var paymentEntity = await repo.Where<PaymentEntity>(p => p.TransactionId == correlationId && p.Status == PaymentStatus.Processed).ToListAsync();
                    paymentEntity.Count.ShouldBe(1); // Verify payment is processed.
                }

                // Check the final state in OrderDbContext to verify that the order is placed.
                using (var serviceScope = build.Services.CreateScope())
                using (var unitOfWork = serviceScope.ServiceProvider.GetRequiredService<IUnitOfWork>())
                using (var repo = unitOfWork.CreateRepository<OrderDbContext>())
                {
                    var orderEntity = await repo.Where<OrderEntity>(o => o.CorrelationId == correlationId && o.Status == OrderStatus.Placed).ToListAsync();
                    orderEntity.Count.ShouldBe(1); // Verify order is placed.
                }

                // Check the final state in InventoryDbContext to verify that the inventory is reserved.
                using (var serviceScope = build.Services.CreateScope())
                using (var unitOfWork = serviceScope.ServiceProvider.GetRequiredService<IUnitOfWork>())
                using (var repo = unitOfWork.CreateRepository<InventoryDbContext>())
                {
                    var inventoryEntity = await repo.Where<InventoryEntity>(i => i.CorrelationId == correlationId && i.Status == InventoryStatus.Reserved).ToListAsync();
                    inventoryEntity.Count.ShouldBe(1); // Verify inventory is reserved.
                }
            }
        }

        [Fact]
        public async Task OrderSaga_ShouldCompensateDuringProcessing()
        {
            // Arrange: Set up an IHostBuilder with necessary services and configurations
            IHostBuilder host = Host.CreateDefaultBuilder().ConfigureServices((services) =>
            {
                // Configure database contexts for Order, Payment, and Inventory test databases
                services.AddDbContextFactory<OrderDbContext>(options =>
                    options.UseTestSqlConnection(_shared, "OrderSaga_ShouldCompensateDuringProcessing_OrderDb"));

                services.AddDbContextFactory<PaymentDbContext>(options =>
                    options.UseTestSqlConnection(_shared, "OrderSaga_ShouldCompensateDuringProcessing_PaymentDb"));

                services.AddDbContextFactory<InventoryDbContext>(options =>
                    options.UseTestSqlConnection(_shared, "OrderSaga_ShouldCompensateDuringProcessing_InventoryDb"));

                // Register services needed for order, payment, and inventory processing
                services.AddTransient<IOrderService, OrderService>();
                services.AddTransient<IPaymentService, PaymentService>();
                services.AddTransient<IInventoryService, InventoryService>();

                // Register repository factory for Unit of Work pattern
                services.AddRepositoryFactory(ServiceLifetime.Transient);

                // Set up MassTransit for testing the saga with consumers and in-memory transport
                services.AddMassTransitTestHarness(x =>
                {
                    x.AddConsumer<OrderConsumer>(); // Add consumers for handling order-related events
                    x.AddConsumer<PaymentConsumer>(); // Add consumer for payment processing
                    x.AddConsumer<InventoryConsumer>(); // Add consumer for inventory management
                    x.AddConsumer<RollbackInventoryConsumer>(); // Add rollback consumer for inventory
                    x.AddConsumer<RollbackPaymentConsumer>(); // Add rollback consumer for payment
                    x.AddConsumer<RollbackOrderConsumer>(); // Add rollback consumer for order

                    // Add the saga state machine for managing the order saga lifecycle
                    x.AddSagaStateMachine<OrderSagaStateMachine, OrderSagaState>()
                        .InMemoryRepository(); // Use in-memory repository for the saga state

                    // Configure MassTransit to use in-memory transport for testing
                    x.UsingInMemory((context, cfg) =>
                    {
                        cfg.ConfigureEndpoints(context); // Automatically configure endpoints
                    });
                });
            });

            // Act: Build the host and start the test harness
            using (IHost build = host.Build())
            {
                // Ensure that the test databases for Order, Payment, and Inventory exist
                build.Services.EnsureDatabaseExists<OrderDbContext>();
                build.Services.EnsureDatabaseExists<PaymentDbContext>();
                build.Services.EnsureDatabaseExists<InventoryDbContext>();

                // Get the MassTransit test harness to simulate message handling
                var harness = build.Services.CreateScope().ServiceProvider.GetRequiredService<ITestHarness>();
                await harness.Start(); // Start MassTransit test harness

                // Get the saga harness to track and verify the saga's state
                var sagaHarness = harness.GetSagaStateMachineHarness<OrderSagaStateMachine, OrderSagaState>();
                var correlationId = Guid.NewGuid(); // Unique ID for the saga instance

                // Publish the event to start the order saga with an order and items
                await harness.Bus.Publish(new StartOrderEvent(correlationId, 100M, new List<OrderItem>
                {
                    new() { ProductId = Guid.NewGuid(), Quantity = 1, Price = 100M }
                }));

                // Assert: Verify that the saga was created and transitioned to Processing state
                Assert.True(await sagaHarness.Created.Any(x => x.CorrelationId == correlationId));
                Assert.True(await harness.Published.Any<StartPaymentEvent>()); // Verify that payment event is published
                Assert.True(await harness.Consumed.Any<StartPaymentEvent>()); // Verify payment event is consumed

                // Simulate a failure that triggers compensation in the saga
                await harness.Bus.Publish(new CompensateOrderEvent(correlationId));

                // Assert: Verify that compensation events are published and consumed
                Assert.True(await harness.Published.Any<CompensateOrderEvent>()); // Verify compensation event published
                Assert.True(await harness.Consumed.Any<CompensateOrderEvent>());

                Assert.True(await harness.Published.Any<RollbackPaymentEvent>()); // Verify payment rollback event published
                Assert.True(await harness.Consumed.Any<RollbackPaymentEvent>());

                Assert.True(await harness.Published.Any<RollbackOrderEvent>()); // Verify order rollback event published
                Assert.True(await harness.Consumed.Any<RollbackOrderEvent>());

                Assert.True(await harness.Published.Any<RollbackInventoryEvent>()); // Verify inventory rollback event published
                Assert.True(await harness.Consumed.Any<RollbackInventoryEvent>());

                // Assert: Verify the saga transitions to Compensated state after the rollback
                var instance = sagaHarness.Created.ContainsInState(correlationId, sagaHarness.StateMachine, sagaHarness.StateMachine.Compensated);
                instance.ShouldNotBeNull(); // Ensure the saga reaches compensated state

                // Assert: Verify the final state of the Payment, Order, and Inventory entities in their respective databases

                // Check PaymentDbContext for the cancelled payment entity
                using (var serviceScope = build.Services.CreateScope())
                using (var unitOfWork = serviceScope.ServiceProvider.GetRequiredService<IUnitOfWork>())
                using (var repo = unitOfWork.CreateRepository<PaymentDbContext>())
                {
                    var paymentEntity = await repo.Where<PaymentEntity>(p => p.TransactionId == correlationId && p.Status == PaymentStatus.Cancelled).ToListAsync();
                    paymentEntity.Count.ShouldBe(1); // Verify payment is cancelled
                }

                // Check OrderDbContext for the cancelled order entity
                using (var serviceScope = build.Services.CreateScope())
                using (var unitOfWork = serviceScope.ServiceProvider.GetRequiredService<IUnitOfWork>())
                using (var repo = unitOfWork.CreateRepository<OrderDbContext>())
                {
                    var orderEntity = await repo.Where<OrderEntity>(o => o.CorrelationId == correlationId && o.Status == OrderStatus.Cancelled).ToListAsync();
                    orderEntity.Count.ShouldBe(1); // Verify order is cancelled
                }

                // Check InventoryDbContext for the cancelled inventory entity
                using (var serviceScope = build.Services.CreateScope())
                using (var unitOfWork = serviceScope.ServiceProvider.GetRequiredService<IUnitOfWork>())
                using (var repo = unitOfWork.CreateRepository<InventoryDbContext>())
                {
                    var inventoryEntity = await repo.Where<InventoryEntity>(i => i.CorrelationId == correlationId && i.Status == InventoryStatus.Cancelled).ToListAsync();
                    inventoryEntity.Count.ShouldBe(1); // Verify inventory reservation is cancelled
                }
            }
        }


        [Fact]
        public async Task OrderSaga_ShouldCompensateDuringInventoryProcessing()
        {
            // Arrange: Set up an IHostBuilder with necessary services and configurations
            IHostBuilder host = Host.CreateDefaultBuilder().ConfigureServices((services) =>
            {
                // Configure database contexts for Order, Payment, and Inventory for testing
                services.AddDbContextFactory<OrderDbContext>(options =>
                    options.UseTestSqlConnection(_shared, "OrderSaga_ShouldCompensateDuringInventoryProcessing_OrderDb"));

                services.AddDbContextFactory<PaymentDbContext>(options =>
                    options.UseTestSqlConnection(_shared, "OrderSaga_ShouldCompensateDuringInventoryProcessing_PaymentDb"));

                services.AddDbContextFactory<InventoryDbContext>(options =>
                    options.UseTestSqlConnection(_shared, "OrderSaga_ShouldCompensateDuringInventoryProcessing_InventoryDb"));

                // Register services needed for order, payment, and inventory processing
                services.AddTransient<IOrderService, OrderService>();
                services.AddTransient<IPaymentService, PaymentService>();

                // Mock IInventoryService to force failure during inventory reservation
                services.AddTransient<IInventoryService>(x =>
                {
                    Mock<InventoryService> mock = new Mock<InventoryService>(x.GetRequiredService<IUnitOfWork>()) { CallBase = true };
                    mock.Setup(f => f.ReserveInventoryAsync(It.IsAny<Guid>(), It.IsAny<List<OrderItem>>()))
                        .ReturnsAsync(false); // Simulate inventory reservation failure
                    return (IInventoryService)mock.Object;
                });

                // Register repository factory for Unit of Work pattern
                services.AddRepositoryFactory(ServiceLifetime.Transient);

                // Set up MassTransit for testing the saga with consumers and in-memory transport
                services.AddMassTransitTestHarness(x =>
                {
                    x.AddConsumer<OrderConsumer>(); // Add consumers for handling events in the saga
                    x.AddConsumer<PaymentConsumer>();
                    x.AddConsumer<InventoryConsumer>();
                    x.AddConsumer<RollbackInventoryConsumer>();
                    x.AddConsumer<RollbackPaymentConsumer>();
                    x.AddConsumer<RollbackOrderConsumer>();

                    // Add saga state machine and configure it to use in-memory repository
                    x.AddSagaStateMachine<OrderSagaStateMachine, OrderSagaState>()
                        .InMemoryRepository();

                    x.UsingInMemory((context, cfg) =>
                    {
                        cfg.ConfigureEndpoints(context); // Automatically configure endpoints
                    });
                });
            });

            // Act: Build the host and start the test harness
            using (IHost build = host.Build())
            {
                // Ensure the databases are created
                build.Services.EnsureDatabaseExists<OrderDbContext>();
                build.Services.EnsureDatabaseExists<PaymentDbContext>();
                build.Services.EnsureDatabaseExists<InventoryDbContext>();

                var harness = build.Services.CreateScope().ServiceProvider.GetRequiredService<ITestHarness>();
                await harness.Start(); // Start MassTransit test harness

                var sagaHarness = harness.GetSagaStateMachineHarness<OrderSagaStateMachine, OrderSagaState>();
                var correlationId = Guid.NewGuid();

                // Publish the event to start the order saga with an order and items
                await harness.Bus.Publish(new StartOrderEvent(correlationId, 100M, new List<OrderItem>
                {
                    new() { ProductId = Guid.NewGuid(), Quantity = 1, Price = 100M }
                }));

                // Assert: Verify that compensation events are published and consumed
                Assert.True(await harness.Published.Any<CompensateOrderEvent>()); // Compensation for order
                Assert.True(await harness.Consumed.Any<CompensateOrderEvent>());

                Assert.True(await harness.Published.Any<RollbackPaymentEvent>()); // Rollback payment
                Assert.True(await harness.Consumed.Any<RollbackPaymentEvent>());

                Assert.True(await harness.Published.Any<RollbackOrderEvent>()); // Rollback order
                Assert.True(await harness.Consumed.Any<RollbackOrderEvent>());

                Assert.True(await harness.Published.Any<RollbackInventoryEvent>()); // Rollback inventory
                Assert.True(await harness.Consumed.Any<RollbackInventoryEvent>());

                // Verify that the saga instance transitioned to the Compensated state
                var instance = sagaHarness.Created.ContainsInState(correlationId, sagaHarness.StateMachine, sagaHarness.StateMachine.Compensated);
                instance.ShouldNotBeNull(); // Ensure the saga reaches compensated state

                // Assert: Verify final states in the databases for Payment, Order, and Inventory

                // Check final state in PaymentDbContext (expect cancelled payment)
                using (var serviceScope = build.Services.CreateScope())
                using (var unitOfWork = serviceScope.ServiceProvider.GetRequiredService<IUnitOfWork>())
                using (var repo = unitOfWork.CreateRepository<PaymentDbContext>())
                {
                    var paymentEntity = await repo.Where<PaymentEntity>(p => p.TransactionId == correlationId && p.Status == PaymentStatus.Cancelled).ToListAsync();
                    paymentEntity.Count.ShouldBe(1); // Ensure payment is cancelled
                }

                // Check final state in OrderDbContext (expect cancelled order)
                using (var serviceScope = build.Services.CreateScope())
                using (var unitOfWork = serviceScope.ServiceProvider.GetRequiredService<IUnitOfWork>())
                using (var repo = unitOfWork.CreateRepository<OrderDbContext>())
                {
                    var orderEntity = await repo.Where<OrderEntity>(o => o.CorrelationId == correlationId && o.Status == OrderStatus.Cancelled).ToListAsync();
                    orderEntity.Count.ShouldBe(1); // Ensure order is cancelled
                }

                // Check final state in InventoryDbContext (expect no inventory reserved)
                using (var serviceScope = build.Services.CreateScope())
                using (var unitOfWork = serviceScope.ServiceProvider.GetRequiredService<IUnitOfWork>())
                using (var repo = unitOfWork.CreateRepository<InventoryDbContext>())
                {
                    var lstInventory = await repo.AsQueryable<InventoryEntity>().ToListAsync();
                    lstInventory.Count.ShouldBe(0); // Ensure no inventory was reserved
                }
            }
        }

    }
}
