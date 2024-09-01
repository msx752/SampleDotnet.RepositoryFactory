using DotNet.Testcontainers.Builders;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

namespace SampleDotnet.RepositoryFactory.Tests.Cases;

public class DateTimeOffsetAsyncTests : IAsyncLifetime
{
    private readonly MsSqlContainer _sqlContainer;
    public DateTimeOffsetAsyncTests()
    {
        _sqlContainer = new MsSqlBuilder()
            .WithPassword("Admin123!")
            .WithCleanUp(true)
            .WithReuse(true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(1433))
            .Build();
    }
    public async Task InitializeAsync()
    {
        await _sqlContainer.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _sqlContainer.StopAsync();
        await _sqlContainer.DisposeAsync();
    }


    [Fact]
    public async Task Case_set_CreatedAt_DateTimeOffsetAsync()
    {
        IHostBuilder host = Host.CreateDefaultBuilder().ConfigureServices((services) =>
        {
            services.AddDbContextFactoryWithUnitOfWork<TestApplicationDbContext>(options =>
            {
                var cnnBuilder = new SqlConnectionStringBuilder(_sqlContainer.GetConnectionString());
                cnnBuilder.InitialCatalog = "Case_set_CreatedAt_DateTimeOffsetAsync";
                cnnBuilder.TrustServerCertificate = true;
                cnnBuilder.MultipleActiveResultSets = true;
                cnnBuilder.ConnectRetryCount = 5;
                cnnBuilder.ConnectTimeout = TimeSpan.FromMinutes(5).Seconds;
                options.UseSqlServer(cnnBuilder.ToString(), (opt) => opt.EnableRetryOnFailure());
                //options.UseInMemoryDatabase("Case_set_CreatedAt_DateTimeOffsetAsync");
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            });
        });


        using (IHost build = host.Build())
        {
            //scope1
            using (IServiceScope scope = build.Services.CreateScope())
            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                var testApplicationDbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<TestApplicationDbContext>>();
                using (var context = testApplicationDbContextFactory.CreateDbContext())
                {
                    context.Database.EnsureCreated();
                    await context.CLEAN_TABLES_DO_NOT_USE_PRODUCTION();
                }

                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                using (IRepository<TestApplicationDbContext> repo = uow.CreateRepository<TestApplicationDbContext>())
                {
                    TestUserEntity userEntity = new();
                    userEntity.Name = "TestName";
                    userEntity.Surname = "TestSurname";

                    await repo.InsertAsync(userEntity, cancellationTokenSource.Token);

                    userEntity.CreatedAt.ShouldNotBeNull();
                }
                await uow.SaveChangesAsync(cancellationTokenSource.Token);
            }
        }
    }

    [Fact]
    public async Task Case_set_UpdatedAt_DateTimeOffsettAsync()
    {
        IHostBuilder host = Host.CreateDefaultBuilder().ConfigureServices((services) =>
        {
            services.AddDbContextFactoryWithUnitOfWork<TestApplicationDbContext>(options =>
            {
                var cnnBuilder = new SqlConnectionStringBuilder(_sqlContainer.GetConnectionString());
                cnnBuilder.InitialCatalog = "Case_set_UpdatedAt_DateTimeOffsetAsync";
                cnnBuilder.TrustServerCertificate = true;
                cnnBuilder.MultipleActiveResultSets = true;
                cnnBuilder.ConnectRetryCount = 5;
                cnnBuilder.ConnectTimeout = TimeSpan.FromMinutes(5).Seconds;
                options.UseSqlServer(cnnBuilder.ToString(), (opt) => opt.EnableRetryOnFailure());
                //options.UseInMemoryDatabase("Case_set_UpdatedAt_DateTimeOffsetAsync");
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            });
        });

        using (IHost build = host.Build())
        {
            var testApplicationDbContextFactory = build.Services.GetRequiredService<IDbContextFactory<TestApplicationDbContext>>();
            using (var context = testApplicationDbContextFactory.CreateDbContext())
            {
                context.Database.EnsureCreated();
                await context.CLEAN_TABLES_DO_NOT_USE_PRODUCTION();
            }

            //scope1
            using (IServiceScope scope = build.Services.CreateScope())
            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                using (IRepository<TestApplicationDbContext> repo = uow.CreateRepository<TestApplicationDbContext>())
                {
                    TestUserEntity userEntity = new();
                    userEntity.Name = "TestName";
                    userEntity.Surname = "TestSurname";

                    userEntity.CreatedAt.ShouldBeNull();
                    userEntity.UpdatedAt.ShouldBeNull();

                    await repo.InsertAsync(userEntity, cancellationTokenSource.Token);

                    userEntity.CreatedAt.ShouldNotBeNull();
                    userEntity.UpdatedAt.ShouldBeNull();
                }
                await uow.SaveChangesAsync(cancellationTokenSource.Token);
            }

            //scope2
            using (IServiceScope scope = build.Services.CreateScope())
            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                using (IRepository<TestApplicationDbContext> repo = uow.CreateRepository<TestApplicationDbContext>())
                {
                    TestUserEntity? userEntity = await repo.FirstOrDefaultAsync<TestUserEntity>(f => f.Name == "TestName" && f.Surname == "TestSurname", cancellationTokenSource.Token);

                    userEntity.ShouldNotBeNull();
                    userEntity.CreatedAt.ShouldNotBeNull();
                    userEntity.UpdatedAt.ShouldBeNull();

                    repo.Update(userEntity);

                    userEntity.CreatedAt.ShouldNotBeNull();
                    userEntity.UpdatedAt.ShouldNotBeNull();
                }
                await uow.SaveChangesAsync(cancellationTokenSource.Token);
            }
        }
    }
}