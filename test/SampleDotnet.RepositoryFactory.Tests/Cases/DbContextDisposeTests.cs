using DotNet.Testcontainers.Builders;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

namespace SampleDotnet.RepositoryFactory.Tests.Cases
{
    public class DbContextDisposeTests : IAsyncLifetime
    {
        private readonly MsSqlContainer _sqlContainer;
        public DbContextDisposeTests()
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
        public async Task Case_DbContext_Should_Not_Throw_ObjectDisposedException()
        {
            IHostBuilder host = Host.CreateDefaultBuilder().ConfigureServices((services) =>
            {
                services.AddDbContextFactoryWithUnitOfWork<TestApplicationDbContext>(options =>
                {
                    var cnnBuilder = new SqlConnectionStringBuilder(_sqlContainer.GetConnectionString());
                    cnnBuilder.InitialCatalog = "Case_DbContext_Should_Not_Throw_ObjectDisposedException";
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

                //request scope
                using (IServiceScope requestScope = build.Services.CreateScope())
                using (var unitOfWork = requestScope.ServiceProvider.GetRequiredService<IUnitOfWork>())
                using (var cancellationTokenSource = new CancellationTokenSource())
                using (var repository = unitOfWork.CreateRepository<TestApplicationDbContext>())
                {
                    var user1 = new TestUserEntity()
                    {
                        Name = "Name",
                        Surname = "Surname",
                    };

                    user1.CreatedAt.ShouldBeNull();
                    await repository.InsertAsync(user1);

                    await unitOfWork.SaveChangesAsync();

                    user1.CreatedAt.ShouldNotBeNull();
                    user1.UpdatedAt.ShouldBeNull();

                    user1.Name = "Name1";
                    repository.Update(user1);

                    await unitOfWork.SaveChangesAsync();

                    user1.UpdatedAt.ShouldNotBeNull();
                }
            }
        }

        //[Fact] //The active test run was aborted. Reason: Test host process crashed
        public async Task Case_Repository_Should_Not_Throw_ObjectDisposedException()
        {
            IHostBuilder host = Host.CreateDefaultBuilder().ConfigureServices((services) =>
            {
                services.AddDbContextFactoryWithUnitOfWork<TestApplicationDbContext>(options =>
                {
                    var cnnBuilder = new SqlConnectionStringBuilder(_sqlContainer.GetConnectionString());
                    cnnBuilder.InitialCatalog = "Case_Repository_Should_Not_Throw_ObjectDisposedException";
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


                //request scope 1
                using (IServiceScope requestScope = build.Services.CreateScope())
                using (var unitOfWork = requestScope.ServiceProvider.GetRequiredService<IUnitOfWork>())
                using (var cancellationTokenSource = new CancellationTokenSource())
                {
                    using (var repository = unitOfWork.CreateRepository<TestApplicationDbContext>())
                    {
                        var user1 = new TestUserEntity()
                        {
                            Name = "Name",
                            Surname = "Surname",
                        };

                        user1.CreatedAt.ShouldBeNull();
                        await repository.InsertAsync(user1);

                        await unitOfWork.SaveChangesAsync();
                        await unitOfWork.SaveChangesAsync();
                    }
                }

                //request scope 2
                using (IServiceScope requestScope = build.Services.CreateScope())
                using (var unitOfWork = requestScope.ServiceProvider.GetRequiredService<IUnitOfWork>())
                using (var cancellationTokenSource = new CancellationTokenSource())
                {
                    Parallel.For(0, 100, new ParallelOptions() { MaxDegreeOfParallelism = 5 }, async (i) =>
                    {
                        using (var repository = unitOfWork.CreateRepository<TestApplicationDbContext>())
                        {
                            var user1 = await repository.FirstOrDefaultAsync<TestUserEntity>(f => f.Surname == "Surname");

                            user1.Name = "Name" + i.ToString("00");

                            await unitOfWork.SaveChangesAsync();

                            repository.Update(user1);

                            await unitOfWork.SaveChangesAsync();
                        }
                    });
                }
            }
        }
    }
}