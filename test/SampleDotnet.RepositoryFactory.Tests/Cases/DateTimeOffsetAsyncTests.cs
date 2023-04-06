namespace SampleDotnet.RepositoryFactory.Tests.Cases;

public class DateTimeOffsetAsyncTests
{
    public DateTimeOffsetAsyncTests()
    {
    }

    [Fact]
    public async Task Case_set_CreatedAt_DateTimeOffsetAsync()
    {
        IHostBuilder host = Host.CreateDefaultBuilder().ConfigureServices((services) =>
        {
            services.AddDbContextFactoryWithUnitOfWork<TestApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase("Case_set_CreatedAt_DateTimeOffsetAsync");
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            });
        });

        IHost b = host.Build();

        //scope1
        using (IServiceScope scope = b.Services.CreateScope())
        using (var cancellationTokenSource = new CancellationTokenSource())
        {
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

    [Fact]
    public async Task Case_set_UpdatedAt_DateTimeOffsettAsync()
    {
        IHostBuilder host = Host.CreateDefaultBuilder().ConfigureServices((services) =>
        {
            services.AddDbContextFactoryWithUnitOfWork<TestApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase("Case_set_UpdatedAt_DateTimeOffsetAsync");
                options.EnableDetailedErrors();
            });
        });

        IHost b = host.Build();

        //scope1
        using (IServiceScope scope = b.Services.CreateScope())
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
        using (IServiceScope scope = b.Services.CreateScope())
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