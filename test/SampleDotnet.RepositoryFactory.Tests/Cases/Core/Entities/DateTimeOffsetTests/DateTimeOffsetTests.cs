namespace SampleDotnet.RepositoryFactory.Tests.Cases.Core.Entities.DateTimeOffsetTests;

public class DateTimeOffsetTests : IAsyncLifetime
{
    private readonly MsSqlContainer _sqlContainer;

    public DateTimeOffsetTests()
    {
        _sqlContainer = new MsSqlBuilder()
            .WithPassword("Admin123!")  // Set the password for the SQL Server.
            .WithCleanUp(true)        // automatically clean up the container.
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(1433))  // Wait strategy to ensure SQL Server is ready.
            .Build();  // Build the container.
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
            services.AddDbContextFactoryWithUnitOfWork<DateTimeOffsetDbContext>(options =>
            {
                var cnnBuilder = new SqlConnectionStringBuilder(_sqlContainer.GetConnectionString());
                cnnBuilder.InitialCatalog = "Case_set_CreatedAt_DateTimeOffset";
                cnnBuilder.TrustServerCertificate = true;
                cnnBuilder.MultipleActiveResultSets = true;
                cnnBuilder.ConnectRetryCount = 5;
                cnnBuilder.ConnectTimeout = TimeSpan.FromMinutes(5).Seconds;
                options.UseSqlServer(cnnBuilder.ToString(), (opt) => opt.EnableRetryOnFailure());
                //options.UseInMemoryDatabase("Case_set_CreatedAt_DateTimeOffset");
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            });
        });

        using (IHost build = host.Build())
        {
            var testApplicationDbContextFactory = build.Services.GetRequiredService<IDbContextFactory<DateTimeOffsetDbContext>>();
            using (var context = testApplicationDbContextFactory.CreateDbContext())
            {
                context.Database.EnsureCreated();
                await context.CLEAN_TABLES_DO_NOT_USE_IN_PRODUCTION();
            }

            //scope1
            using (IServiceScope scope = build.Services.CreateScope())
            {
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                using (IRepository<DateTimeOffsetDbContext> repo = uow.CreateRepository<DateTimeOffsetDbContext>())
                {
                    DateTimeOffsetDbEntity userEntity = new();
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
            services.AddDbContextFactoryWithUnitOfWork<DateTimeOffsetDbContext>(options =>
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
            var testApplicationDbContextFactory = build.Services.GetRequiredService<IDbContextFactory<DateTimeOffsetDbContext>>();
            using (var context = testApplicationDbContextFactory.CreateDbContext())
            {
                context.Database.EnsureCreated();
                await context.CLEAN_TABLES_DO_NOT_USE_IN_PRODUCTION();
            }

            //scope1
            using (IServiceScope scope = build.Services.CreateScope())
            {
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                using (IRepository<DateTimeOffsetDbContext> repo = uow.CreateRepository<DateTimeOffsetDbContext>())
                {
                    DateTimeOffsetDbEntity userEntity = new();
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
                using (IRepository<DateTimeOffsetDbContext> repo = uow.CreateRepository<DateTimeOffsetDbContext>())
                {
                    DateTimeOffsetDbEntity? userEntity = repo.FirstOrDefault<DateTimeOffsetDbEntity>(f => f.Name == "TestName" && f.Surname == "TestSurname");

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