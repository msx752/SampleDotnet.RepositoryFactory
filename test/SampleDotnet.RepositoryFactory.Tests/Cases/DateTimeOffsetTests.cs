using DotNet.Testcontainers.Builders;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

namespace SampleDotnet.RepositoryFactory.Tests.Cases;

public class DateTimeOffsetTests : IAsyncLifetime
{
    private readonly MsSqlContainer _sqlContainer;
    public DateTimeOffsetTests()
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
    public async void Case_set_CreatedAt_DateTimeOffset()
    {
        IHostBuilder host = Host.CreateDefaultBuilder().ConfigureServices((services) =>
        {
            services.AddDbContextFactoryWithUnitOfWork<TestApplicationDbContext>(options =>
            {
                var cnnBuilder = new SqlConnectionStringBuilder(_sqlContainer.GetConnectionString());
                cnnBuilder.InitialCatalog = "Case_set_CreatedAt_DateTimeOffset";
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
                await context.CleanUpTableRecordAsync();
            }

            //scope1
            using (IServiceScope scope = build.Services.CreateScope())
            {

                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                using (IRepository<TestApplicationDbContext> repo = uow.CreateRepository<TestApplicationDbContext>())
                {
                    TestUserEntity userEntity = new();
                    userEntity.Name = "TestName";
                    userEntity.Surname = "TestSurname";

                    repo.Insert(userEntity);

                    userEntity.CreatedAt.ShouldNotBeNull();
                }
                uow.SaveChanges();
            }
        }
    }

    [Fact]
    public async Task Case_set_UpdatedAt_DateTimeOffset()
    {
        IHostBuilder host = Host.CreateDefaultBuilder().ConfigureServices((services) =>
        {
            services.AddDbContextFactoryWithUnitOfWork<TestApplicationDbContext>(options =>
            {
                var cnnBuilder = new SqlConnectionStringBuilder(_sqlContainer.GetConnectionString());
                cnnBuilder.InitialCatalog = "Case_set_UpdatedAt_DateTimeOffset";
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
                await context.CleanUpTableRecordAsync();
            }

            //scope1
            using (IServiceScope scope = build.Services.CreateScope())
            {
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                using (IRepository<TestApplicationDbContext> repo = uow.CreateRepository<TestApplicationDbContext>())
                {
                    TestUserEntity userEntity = new();
                    userEntity.Name = "TestName";
                    userEntity.Surname = "TestSurname";

                    userEntity.CreatedAt.ShouldBeNull();
                    userEntity.UpdatedAt.ShouldBeNull();

                    repo.Insert(userEntity);

                    userEntity.CreatedAt.ShouldNotBeNull();
                    userEntity.UpdatedAt.ShouldBeNull();
                }
                uow.SaveChanges();
            }

            //scope2
            using (IServiceScope scope = build.Services.CreateScope())
            {
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                using (IRepository<TestApplicationDbContext> repo = uow.CreateRepository<TestApplicationDbContext>())
                {
                    TestUserEntity? userEntity = repo.FirstOrDefault<TestUserEntity>(f => f.Name == "TestName" && f.Surname == "TestSurname");

                    userEntity.ShouldNotBeNull();
                    userEntity.CreatedAt.ShouldNotBeNull();
                    userEntity.UpdatedAt.ShouldBeNull();

                    repo.Update(userEntity);

                    userEntity.CreatedAt.ShouldNotBeNull();
                    userEntity.UpdatedAt.ShouldNotBeNull();
                }
                uow.SaveChanges();
            }
        }
    }
}