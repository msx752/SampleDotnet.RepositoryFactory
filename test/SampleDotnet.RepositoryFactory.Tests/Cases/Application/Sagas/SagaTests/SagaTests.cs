using SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels;
using SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.Consumers;
using SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.DbContexts;
using SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.Entities;
using SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.Enums;
using SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.EventMessages;
using SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.Events;
using SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.Interfaces;
using SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.Services;

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
            services.AddDbContextFactory<TestCartDbContext>(options =>
                options.UseTestSqlConnection(_shared, "CartDbContext_Saga_ShouldCommitOnSuccess"));

            services.AddDbContextFactory<TestPaymentDbContext>(options =>
                options.UseTestSqlConnection(_shared, "PaymentDbContext_Saga_ShouldCommitOnSuccess"));

            // Register application services.
            services.AddTransient<ITestCartService, TestCartService_Success>();
            services.AddTransient<ITestPaymentService, TestPaymentService>();
            services.AddRepositoryFactory(ServiceLifetime.Transient);

            // Configure MassTransit with test harness for in-memory transport.
            services.AddMassTransitTestHarness(x =>
            {
                x.AddConsumer<TestCartConsumer>();
                x.AddConsumer<TestPaymentConsumer>();

                x.UsingInMemory((context, cfg) =>
                {
                    cfg.ConfigureEndpoints(context);
                });

                x.AddSagaStateMachine<TestTransactionStateMachine, TestTransactionState>()
                    .InMemoryRepository();
            });
        });

        using (IHost build = host.Build())
        {
            build.Services.EnsureDatabaseExists<TestCartDbContext>();
            build.Services.EnsureDatabaseExists<TestPaymentDbContext>();

            var harness = build.Services.CreateScope().ServiceProvider.GetRequiredService<ITestHarness>();
            await harness.Start();

            using (IServiceScope requestScope = build.Services.CreateScope())
            {
                var correlationId = Guid.NewGuid();


                await harness.Bus.Publish(new StartTransactionEvent(correlationId, 100M, new() { new() { ProductId = Guid.NewGuid(), Quantity = 2, Price = 20M } }));
                (await harness.Consumed.Any<StartTransactionEvent>()).ShouldBeTrue();


                var sagaHarness = harness.GetSagaStateMachineHarness<TestTransactionStateMachine, TestTransactionState>();
                var instance = sagaHarness.Created.ContainsInState(correlationId, sagaHarness.StateMachine, sagaHarness.StateMachine.Processing);
                instance.ShouldNotBeNull();


                await harness.Bus.Publish(new CompletePaymentEvent(correlationId));
                (await harness.Consumed.Any<CompletePaymentEvent>()).ShouldBeTrue();


                (await harness.Published.Any<StartCartEvent>()).ShouldBeTrue();


                await harness.Bus.Publish(new CompleteCartEvent(correlationId));
                (await harness.Consumed.Any<CompleteCartEvent>()).ShouldBeTrue();


                sagaHarness = harness.GetSagaStateMachineHarness<TestTransactionStateMachine, TestTransactionState>();
                instance = sagaHarness.Created.ContainsInState(correlationId, sagaHarness.StateMachine, sagaHarness.StateMachine.Completed);
                instance.ShouldNotBeNull();
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
            services.AddDbContextFactory<TestCartDbContext>(options =>
                options.UseTestSqlConnection(_shared, "CartDbContext_Saga_ShouldRollbackOnFailure"));

            // Configure PaymentDbContext with SQL Server settings for testing.
            services.AddDbContextFactory<TestPaymentDbContext>(options => 
                options.UseTestSqlConnection(_shared, "PaymentDbContext_Saga_ShouldRollbackOnFailure"));

            // Register application services.
            services.AddTransient<ITestCartService, TestCartService_Fail>();
            services.AddTransient<ITestPaymentService, TestPaymentService>();
            services.AddRepositoryFactory(ServiceLifetime.Transient);

            // Configure MassTransit with test harness for in-memory transport.
            services.AddMassTransitTestHarness(x =>
            {
                x.AddConsumer<TestCartConsumer>();
                x.AddConsumer<TestPaymentConsumer>();

                x.UsingInMemory((context, cfg) =>
                {
                    cfg.ConfigureEndpoints(context);
                });

                x.AddSagaStateMachine<TestTransactionStateMachine, TestTransactionState>()
                    .InMemoryRepository();
            });
        });

        using (IHost build = host.Build())
        {
            build.Services.EnsureDatabaseExists<TestCartDbContext>();
            build.Services.EnsureDatabaseExists<TestPaymentDbContext>();

            var harness = build.Services.CreateScope().ServiceProvider.GetRequiredService<ITestHarness>();
            await harness.Start();

            using (IServiceScope requestScope = build.Services.CreateScope())
            {
                var correlationId = Guid.NewGuid();


                await harness.Bus.Publish(new StartTransactionEvent(correlationId, 100M, new() { new() { ProductId = Guid.NewGuid(), Quantity = 2, Price = 20M } }));
                (await harness.Consumed.Any<StartTransactionEvent>()).ShouldBeTrue();


                var sagaHarness = harness.GetSagaStateMachineHarness<TestTransactionStateMachine, TestTransactionState>();
                var instance = sagaHarness.Created.ContainsInState(correlationId, sagaHarness.StateMachine, sagaHarness.StateMachine.Processing);
                instance.ShouldNotBeNull();


                await harness.Bus.Publish(new CompletePaymentEvent(correlationId));


                (await harness.Consumed.Any<CompletePaymentEvent>()).ShouldBeTrue();
                (await harness.Published.Any<StartCartEvent>()).ShouldBeTrue();
                (await harness.Published.Any<CompensateTransactionEvent>()).ShouldBeTrue();
                (await harness.Consumed.Any<RollbackCartEvent>()).ShouldBeTrue();
                (await harness.Consumed.Any<RollbackPaymentEvent>()).ShouldBeTrue();


                using (var scope = build.Services.CreateScope())
                using (var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>())
                using (var paymentRepo = unitOfWork.CreateRepository<TestPaymentDbContext>())
                {
                    var paymentEntity = await paymentRepo.Where<TestPaymentEntity>(p => p.TransactionId == correlationId && p.Status == PaymentStatus.Cancelled).ToListAsync();
                    paymentEntity.Count.ShouldBe(1);
                }

                using (var scope = build.Services.CreateScope())
                using (var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>())
                using (var cartRepo = unitOfWork.CreateRepository<TestCartDbContext>())
                {
                    var cartEntity = await cartRepo.Where<TestCartEntity>(p => p.TransactionId == correlationId).ToListAsync();
                    cartEntity.Count.ShouldBe(0);
                }
            }
        }
    }
}
