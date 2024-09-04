using SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.WithMultipleServices;

namespace SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaTests;

[Collection("Shared Collection")]
public class SagaWithMultipleServicesTests
{
    // A container for running SQL Server in Docker for testing purposes.
    private readonly SharedContainerFixture _shared;

    // Constructor initializes the SQL container with specific configurations.
    public SagaWithMultipleServicesTests(SharedContainerFixture fixture)
    {
        _shared = fixture;
    }
    [Fact]
    public async Task Saga_ShouldCommitOnSuccess_WithMultipleServices()
    {
        // Create an IHostBuilder and configure services to use DbContexts and messaging.
        IHostBuilder host = Host.CreateDefaultBuilder().ConfigureServices((services) =>
        {
            // Configure DbContexts with SQL Server settings for testing.
            services.AddDbContextFactory<TestCartDbContext>(options =>
                options.UseTestSqlConnection(_shared, "CartDbContext_Saga_ShouldCommitOnSuccess_WithMultipleServices"));

            services.AddDbContextFactory<TestPaymentDbContext>(options =>
                options.UseTestSqlConnection(_shared, "PaymentDbContext_Saga_ShouldCommitOnSuccess_WithMultipleServices"));

            services.AddDbContextFactory<TestInventoryDbContext>(options =>
                options.UseTestSqlConnection(_shared, "InventoryDbContext_Saga_ShouldCommitOnSuccess_WithMultipleServices"));

            // Register application services.
            services.AddTransient<ITestCartService, SagaModels.WithMultipleServices.TestCartService_Success>();
            services.AddTransient<ITestPaymentService, TestPaymentService>();
            services.AddTransient<ITestInventoryService, TestInventoryService_Success>();
            services.AddRepositoryFactory(ServiceLifetime.Transient);

            // Configure MassTransit with test harness for in-memory transport.
            services.AddMassTransitTestHarness(x =>
            {
                x.AddConsumer<TestCartConsumer>();
                x.AddConsumer<TestPaymentConsumer>();
                x.AddConsumer<TestInventoryConsumer>();

                x.UsingInMemory((context, cfg) =>
                {
                    cfg.ConfigureEndpoints(context);
                });

                x.AddSagaStateMachine<TestTransactionStateMachine_WithMultipleServices, SagaModels.WithMultipleServices.TestTransactionState>()
                    .InMemoryRepository();
            });
        });

        using (IHost build = host.Build())
        {
            build.Services.EnsureDatabaseExists<TestCartDbContext>();
            build.Services.EnsureDatabaseExists<TestPaymentDbContext>();
            build.Services.EnsureDatabaseExists<TestInventoryDbContext>();

            var harness = build.Services.CreateScope().ServiceProvider.GetRequiredService<ITestHarness>();
            await harness.Start();

            using (IServiceScope requestScope = build.Services.CreateScope())
            {
                var correlationId = Guid.NewGuid();

                // Publish StartTransactionEvent to initiate the saga
                List<SagaCartItem> items = new()
                {
                    new() { ProductId = Guid.NewGuid(), Quantity = 2, Price = 20M }
                };
                await harness.Bus.Publish(new StartTransactionEvent(correlationId, 100M, items));
                (await harness.Consumed.Any<StartTransactionEvent>()).ShouldBeTrue();

                // Verify saga is in Processing state after StartTransactionEvent
                var sagaHarness = harness.GetSagaStateMachineHarness<TestTransactionStateMachine_WithMultipleServices, SagaModels.WithMultipleServices.TestTransactionState>();
                var instance = sagaHarness.Created.ContainsInState(correlationId, sagaHarness.StateMachine, sagaHarness.StateMachine.Processing);
                instance.ShouldNotBeNull();

                (await harness.Published.Any<StartPaymentEvent>()).ShouldBeTrue();
                (await harness.Consumed.Any<StartPaymentEvent>()).ShouldBeTrue();


                (await harness.Published.Any<CompletePaymentEvent>()).ShouldBeTrue();
                (await harness.Published.Any<StartCartEvent>()).ShouldBeTrue();
                (await harness.Consumed.Any<StartCartEvent>()).ShouldBeTrue();
                (await harness.Consumed.Any<CompletePaymentEvent>()).ShouldBeTrue();


                // Publish CompletePaymentEvent and verify subsequent actions
                (await harness.Published.Any<CompleteCartEvent>()).ShouldBeTrue();
                (await harness.Published.Any<StartInventoryEvent>()).ShouldBeTrue();
                (await harness.Consumed.Any<StartInventoryEvent>()).ShouldBeTrue();
                (await harness.Published.Any<CompleteInventoryEvent>()).ShouldBeTrue();
                (await harness.Published.Any<FinalizeTransactionEvent>()).ShouldBeTrue();
                (await harness.Consumed.Any<FinalizeTransactionEvent>()).ShouldBeTrue();


                using (var scope = build.Services.CreateScope())
                using (var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>())
                using (var paymentRepo = unitOfWork.CreateRepository<TestPaymentDbContext>())
                {
                    var paymentEntity = await paymentRepo.Where<TestPaymentEntity>(p => p.TransactionId == correlationId && p.Status == PaymentStatus.Processed).ToListAsync();
                    paymentEntity.Count.ShouldBe(1);
                }

                using (var scope = build.Services.CreateScope())
                using (var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>())
                using (var cartRepo = unitOfWork.CreateRepository<TestCartDbContext>())
                {
                    var cartEntity = await cartRepo.Where<TestCartEntity>(p => p.CorrelationId == correlationId && p.Status == CartStatus.Reserved).ToListAsync();
                    cartEntity.Count.ShouldBe(1);
                }

                using (var scope = build.Services.CreateScope())
                using (var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>())
                using (var cartRepo = unitOfWork.CreateRepository<TestInventoryDbContext>())
                {
                    var cartEntity = await cartRepo.Where<TestInventoryEntity>(p => p.CorrelationId == correlationId && p.Status == InventoryStatus.Reserved).ToListAsync();
                    cartEntity.Count.ShouldBe(1);
                }

                // Verify saga is in Completed state after all events are processed
                instance = sagaHarness.Created.ContainsInState(correlationId, sagaHarness.StateMachine, sagaHarness.StateMachine.Completed);
                instance.ShouldNotBeNull();
            }
        }
    }
}
