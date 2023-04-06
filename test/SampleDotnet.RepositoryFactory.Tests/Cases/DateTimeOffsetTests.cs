namespace SampleDotnet.RepositoryFactory.Tests.Cases;

public class DateTimeOffsetTests
{
    public DateTimeOffsetTests()
    {
    }

    [Fact]
    public void Case_set_CreatedAt_DateTimeOffset()
    {
        IHostBuilder host = Host.CreateDefaultBuilder().ConfigureServices((services) =>
        {
            services.AddDbContextFactoryWithUnitOfWork<TestApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase("Case_set_CreatedAt_DateTimeOffset");
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            });
        });

        IHost b = host.Build();

        //scope1
        using (IServiceScope scope = b.Services.CreateScope())
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

    [Fact]
    public void Case_set_UpdatedAt_DateTimeOffset()
    {
        IHostBuilder host = Host.CreateDefaultBuilder().ConfigureServices((services) =>
        {
            services.AddDbContextFactoryWithUnitOfWork<TestApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase("Case_set_UpdatedAt_DateTimeOffset");
                options.EnableDetailedErrors();
            });
        });

        IHost b = host.Build();

        //scope1
        using (IServiceScope scope = b.Services.CreateScope())
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
        using (IServiceScope scope = b.Services.CreateScope())
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