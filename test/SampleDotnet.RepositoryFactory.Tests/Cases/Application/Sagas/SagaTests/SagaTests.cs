namespace SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaTests;

[Collection("Shared Collection")]
public class SagaTests
{
    // A container for running SQL Server in Docker for testing purposes.
    private readonly SharedContainerFixture _shared;

    // Constructor initializes the SQL container with specific configurations.
    public SagaTests(SharedContainerFixture fixture)
    {
        _shared = fixture;
    }

    [Fact]
    public async Task Saga_ShouldCommitOnSuccess()
    {
        // Create an IHostBuilder and configure services to use DbContexts and messaging.
        IHostBuilder host = Host.CreateDefaultBuilder().ConfigureServices((services) =>
        {
            // Configure CartDbContext with SQL Server settings for testing.
            services.AddDbContextFactory<CartDbContext>(options =>
            {
                options.UseTestSqlConnection(_shared, "CartDbContext_Saga_ShouldCommitOnSuccess");
            });

            // Configure PaymentDbContext with SQL Server settings for testing.
            services.AddDbContextFactory<PaymentDbContext>(options =>
            {
                options.UseTestSqlConnection(_shared, "PaymentDbContext_Saga_ShouldCommitOnSuccess");
            });

            // Register application services.
            services.AddTransient<ICartService, CartService>();
            services.AddTransient<IPaymentService, PaymentService>();
            services.AddRepositoryFactory(ServiceLifetime.Transient);

            // Configure MassTransit with test harness for in-memory transport.
            services.AddMassTransitTestHarness(x =>
            {
                x.AddConsumer<CartConsumer>();
                x.AddConsumer<PaymentConsumer>();

                x.UsingInMemory((context, cfg) =>
                {
                    cfg.ConfigureEndpoints(context);
                });

                x.AddSagaStateMachine<TransactionStateMachine, TransactionState>()
                    .InMemoryRepository();
            });
        });

        using (IHost build = host.Build())
        {
            build.Services.EnsureDatabaseExists<CartDbContext>();
            build.Services.EnsureDatabaseExists<PaymentDbContext>();

            var harness = build.Services.CreateScope().ServiceProvider.GetRequiredService<ITestHarness>();
            await harness.Start();

            using (IServiceScope requestScope = build.Services.CreateScope())
            {
                var correlationId = Guid.NewGuid();

                await PublishAndVerifyStartTransaction(harness, correlationId);
                await VerifySagaProcessingState(harness, correlationId);
                await SimulateAndVerifyCompletePayment(harness, correlationId);
                await SimulateAndVerifyCompleteCart(harness, correlationId);
                await VerifySagaCompletedState(harness, correlationId);
            }
        }
    }

    [Fact]
    public async Task Saga_ShouldRollbackOnFailure()
    {
        // Create an IHostBuilder and configure services to use DbContexts and messaging.
        IHostBuilder host = Host.CreateDefaultBuilder().ConfigureServices((services) =>
        {
            // Configure CartDbContext with SQL Server settings for testing.
            services.AddDbContextFactory<CartDbContext>(options =>
            {
                options.UseTestSqlConnection(_shared, "CartDbContext_Saga_ShouldRollbackOnFailure");
            });

            // Configure PaymentDbContext with SQL Server settings for testing.
            services.AddDbContextFactory<PaymentDbContext>(options =>
            {
                options.UseTestSqlConnection(_shared, "PaymentDbContext_Saga_ShouldRollbackOnFailure");
            });

            // Register application services.
            services.AddTransient<ICartService, CartService>();
            services.AddTransient<IPaymentService, PaymentService>();
            services.AddRepositoryFactory(ServiceLifetime.Transient);

            // Configure MassTransit with test harness for in-memory transport.
            services.AddMassTransitTestHarness(x =>
            {
                x.AddConsumer<CartConsumer>();
                x.AddConsumer<PaymentConsumer>();

                x.UsingInMemory((context, cfg) =>
                {
                    cfg.ConfigureEndpoints(context);
                });

                x.AddSagaStateMachine<TransactionStateMachine, TransactionState>()
                    .InMemoryRepository();
            });
        });

        using (IHost build = host.Build())
        {
            build.Services.EnsureDatabaseExists<CartDbContext>();
            build.Services.EnsureDatabaseExists<PaymentDbContext>();

            var harness = build.Services.CreateScope().ServiceProvider.GetRequiredService<ITestHarness>();
            await harness.Start();

            using (IServiceScope requestScope = build.Services.CreateScope())
            {
                var correlationId = Guid.NewGuid();

                await PublishAndVerifyStartTransaction(harness, correlationId);
                await VerifySagaProcessingState(harness, correlationId);
                await SimulateAndVerifyCompletePayment(harness, correlationId);
                await SimulateAndVerifyCompensation(harness, correlationId);
                await VerifySagaRolledbackState(harness, correlationId);
                await VerifyDatabaseStateAfterRollback(build, correlationId);
            }
        }
    }

    private static async Task PublishAndVerifyStartTransaction(ITestHarness harness, Guid correlationId)
    {
        await harness.Bus.Publish(new StartTransactionEvent(correlationId, 100M, new List<SagaCartItem>
        {
            new SagaCartItem { ProductId = Guid.NewGuid(), Quantity = 2, Price = 20M }
        }));

        (await harness.Consumed.Any<StartTransactionEvent>()).ShouldBeTrue();
    }

    private static async Task VerifySagaProcessingState(ITestHarness harness, Guid correlationId)
    {
        var sagaHarness = harness.GetSagaStateMachineHarness<TransactionStateMachine, TransactionState>();
        var instance = await sagaHarness.Created.ContainsInState(correlationId, sagaHarness.StateMachine, sagaHarness.StateMachine.Processing);
        instance.ShouldNotBeNull();
    }

    private static async Task SimulateAndVerifyCompletePayment(ITestHarness harness, Guid correlationId)
    {
        await harness.Bus.Publish(new CompletePaymentEvent(correlationId));
        (await harness.Consumed.Any<CompletePaymentEvent>()).ShouldBeTrue();

        (await harness.Published.Any<StartCartEvent>()).ShouldBeTrue();
    }

    private static async Task SimulateAndVerifyCompleteCart(ITestHarness harness, Guid correlationId)
    {
        await harness.Bus.Publish(new CompleteCartEvent(correlationId));
        (await harness.Consumed.Any<CompleteCartEvent>()).ShouldBeTrue();
    }

    private static async Task VerifySagaCompletedState(ITestHarness harness, Guid correlationId)
    {
        var sagaHarness = harness.GetSagaStateMachineHarness<TransactionStateMachine, TransactionState>();
        var instance = await sagaHarness.Created.ContainsInState(correlationId, sagaHarness.StateMachine, sagaHarness.StateMachine.Completed);
        instance.ShouldNotBeNull();
    }

    private static async Task SimulateAndVerifyCompensation(ITestHarness harness, Guid correlationId)
    {
        await harness.Bus.Publish(new CompensateTransactionEvent(correlationId));
        (await harness.Consumed.Any<CompensateTransactionEvent>()).ShouldBeTrue();

        (await harness.Published.Any<RollbackPaymentEvent>()).ShouldBeTrue();
        (await harness.Published.Any<RollbackCartEvent>()).ShouldBeTrue();
    }

    private static async Task VerifySagaRolledbackState(ITestHarness harness, Guid correlationId)
    {
        var sagaHarness = harness.GetSagaStateMachineHarness<TransactionStateMachine, TransactionState>();
        var instance = await sagaHarness.Created.ContainsInState(correlationId, sagaHarness.StateMachine, sagaHarness.StateMachine.Rolledback);
        instance.ShouldNotBeNull();
    }

    private static async Task VerifyDatabaseStateAfterRollback(IHost host, Guid correlationId)
    {
        using var scope = host.Services.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        using (var paymentRepo = unitOfWork.CreateRepository<PaymentDbContext>())
        {
            var paymentEntity = await paymentRepo.Where<PaymentEntity>(p => p.TransactionId == correlationId && p.Status == PaymentStatus.Cancelled).ToListAsync();
            paymentEntity.Count.ShouldBe(1);
        }

        using (var cartRepo = unitOfWork.CreateRepository<CartDbContext>())
        {
            var cartEntity = await cartRepo.Where<CartEntity>(p => p.TransactionId == correlationId && p.Status == CartStatus.Cancelled).ToListAsync();
            cartEntity.Count.ShouldBe(1);
        }
    }
}
