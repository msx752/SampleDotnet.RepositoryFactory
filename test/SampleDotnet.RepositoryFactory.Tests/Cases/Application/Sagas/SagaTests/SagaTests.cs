namespace SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaTests;

public class SagaTests : IAsyncLifetime
{
    // A container for running SQL Server in Docker for testing purposes.
    private readonly MsSqlContainer _sqlContainer;

    // Constructor initializes the SQL container with specific configurations.
    public SagaTests()
    {
        _sqlContainer = new MsSqlBuilder()
            .WithPassword("Admin123!")  // Set the password for the SQL Server.
            .WithCleanUp(true)        // automatically clean up the container.
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(1433))  // Wait strategy to ensure SQL Server is ready.
            .Build();  // Build the container.
    }

    // DisposeAsync stops and disposes of the SQL container asynchronously after each test.
    public async Task DisposeAsync()
    {
        await _sqlContainer.StopAsync();  // Stop the SQL Server container.
        await _sqlContainer.DisposeAsync();  // Dispose of the SQL Server container.
    }

    // InitializeAsync starts the SQL container asynchronously before each test.
    public async Task InitializeAsync()
    {
        await _sqlContainer.StartAsync();  // Start the SQL Server container.
    }

    [Fact]
    public async Task DistributedTransaction_SagaCommitAndRollback()
    {
        // Create an IHostBuilder and configure services to use DbContexts and messaging.
        IHostBuilder host = Host.CreateDefaultBuilder().ConfigureServices((services) =>
        {
            // Configure CartDbContext with SQL Server settings.
            services.AddDbContextFactory<CartDbContext>(options =>
            {
                var cnnBuilder = new SqlConnectionStringBuilder(_sqlContainer.GetConnectionString());
                cnnBuilder.InitialCatalog = "CartDbContext_DistributedTransaction_SagaCommitAndRollback";
                cnnBuilder.TrustServerCertificate = true;
                cnnBuilder.MultipleActiveResultSets = true;
                cnnBuilder.ConnectRetryCount = 5;
                cnnBuilder.ConnectTimeout = TimeSpan.FromMinutes(5).Seconds;
                options.UseSqlServer(cnnBuilder.ToString(), opt => opt.EnableRetryOnFailure());
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            });

            // Configure SecondDbContext with SQL Server settings.
            services.AddDbContextFactory<PaymentDbContext>(options =>
            {
                var cnnBuilder = new SqlConnectionStringBuilder(_sqlContainer.GetConnectionString());
                cnnBuilder.InitialCatalog = "PaymentDbContext_DistributedTransaction_SagaCommitAndRollback";  // Set the initial catalog (database name).
                cnnBuilder.TrustServerCertificate = true;  // Trust the server certificate.
                cnnBuilder.MultipleActiveResultSets = true;  // Allow multiple active result sets.
                cnnBuilder.ConnectRetryCount = 5;  // Set the number of retry attempts for connection.
                cnnBuilder.ConnectTimeout = TimeSpan.FromMinutes(5).Seconds;  // Set connection timeout.
                options.UseSqlServer(cnnBuilder.ToString(), (opt) => opt.EnableRetryOnFailure());  // Use SQL Server with retry on failure.
                options.EnableSensitiveDataLogging();  // Enable logging of sensitive data (for debugging purposes).
                options.EnableDetailedErrors();  // Enable detailed error messages (for debugging purposes).
            });

            services.AddTransient<ICartService, CartService>();
            services.AddTransient<IPaymentService, PaymentService>();
            services.AddRepositoryFactory(ServiceLifetime.Transient);

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
            var CartDbContextFactory = build.Services.GetRequiredService<IDbContextFactory<CartDbContext>>();
            using (var context = CartDbContextFactory.CreateDbContext())
                context.Database.EnsureCreated();

            var paymentDbContextFactory = build.Services.GetRequiredService<IDbContextFactory<PaymentDbContext>>();
            using (var context = paymentDbContextFactory.CreateDbContext())
                context.Database.EnsureCreated();

            var harness = build.Services.CreateScope().ServiceProvider.GetRequiredService<ITestHarness>();
            await harness.Start();

            using (IServiceScope requestScope = build.Services.CreateScope())
            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                var correlationId = Guid.NewGuid();

                // 1. Publish the StartTransaction message to start the saga
                await harness.Bus.Publish(new StartTransactionEvent(correlationId, 100M, new List<SagaCartItem>
                {
                    new SagaCartItem { ProductId = Guid.NewGuid(), Quantity = 2, Price = 20M }
                }), cancellationTokenSource.Token);

                // Verify StartTransaction is consumed
                (await harness.Consumed.Any<StartTransactionEvent>()).ShouldBeTrue();

                var sagaHarness = harness.GetSagaStateMachineHarness<TransactionStateMachine, TransactionState>();

                // Ensure saga instance is created
                (await sagaHarness.Created.Any(x => x.CorrelationId == correlationId)).ShouldBeTrue();

                // 2. Verify that saga is in the Processing state
                var instance = sagaHarness.Created.ContainsInState(correlationId, sagaHarness.StateMachine, sagaHarness.StateMachine.Processing);
                instance.ShouldNotBeNull();

                // 3. Since StartTransaction triggers StartPayment, verify StartPayment is published
                (await harness.Published.Any<StartPaymentEvent>()).ShouldBeTrue();

                // Simulate receiving CompletePayment event
                await harness.Bus.Publish(new CompletePaymentEvent(correlationId));

                // Verify CompletePayment is consumed
                (await harness.Consumed.Any<CompletePaymentEvent>()).ShouldBeTrue();

                // 4. Verify StartCart is published as a result of CompletePayment
                (await harness.Published.Any<StartCartEvent>()).ShouldBeTrue();

                // Simulate receiving CompleteCart event
                await harness.Bus.Publish(new CompleteCartEvent(correlationId));

                // Verify CompleteCart is consumed
                (await harness.Consumed.Any<CompleteCartEvent>()).ShouldBeTrue();

                // 5. Ensure saga is in Completed state after processing CompleteCart
                instance = sagaHarness.Created.ContainsInState(correlationId, sagaHarness.StateMachine, sagaHarness.StateMachine.Completed);
                instance.ShouldNotBeNull();

                // Simulate a failure after payment, triggering compensation
                await harness.Bus.Publish(new CompensateTransactionEvent(correlationId));

                // Verify CompensateTransactionEvent is consumed
                (await harness.Consumed.Any<CompensateTransactionEvent>()).ShouldBeTrue();

                // 3. Verify that Rollback actions are published as a result of compensation
                (await harness.Published.Any<RollbackPaymentEvent>()).ShouldBeTrue();
                (await harness.Published.Any<RollbackCartEvent>()).ShouldBeTrue();

                // 4. Ensure saga is in Rolledback state after rollback actions
                instance = sagaHarness.Created.ContainsInState(correlationId, sagaHarness.StateMachine, sagaHarness.StateMachine.Rolledback);
                instance.ShouldNotBeNull();

                (await harness.Consumed.Any<RollbackPaymentEvent>()).ShouldBeTrue();
                (await harness.Consumed.Any<CompleteCartEvent>()).ShouldBeTrue();

                using (var unitOfWork = build.Services.CreateScope().ServiceProvider.GetRequiredService<IUnitOfWork>())
                using (IRepository<PaymentDbContext> repo = unitOfWork.CreateRepository<PaymentDbContext>())
                {
                    var paymentEntity = await repo.Where<PaymentEntity>(p => p.TransactionId == correlationId && p.Status == PaymentStatus.Cancelled).ToListAsync();
                    paymentEntity.Count.ShouldBeEquivalentTo(1);
                }

                using (var unitOfWork = build.Services.CreateScope().ServiceProvider.GetRequiredService<IUnitOfWork>())
                using (IRepository<CartDbContext> repo = unitOfWork.CreateRepository<CartDbContext>())
                {
                    var cartEntity = await repo.Where<CartEntity>(p => p.TransactionId == correlationId && p.Status == CartStatus.Cancelled).ToListAsync();
                    cartEntity.Count.ShouldBeEquivalentTo(0);
                }
            }
        }
    }
}