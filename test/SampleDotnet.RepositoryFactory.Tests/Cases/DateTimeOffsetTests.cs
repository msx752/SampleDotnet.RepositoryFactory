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
            services.AddDbContextFactory<TestApplicationDbContext>(options =>
                options.UseInMemoryDatabase("Case_set_CreatedAt_DateTimeOffset"));
        });

        IHost b = host.Build();

        //scope1
        using (IServiceScope scope = b.Services.CreateScope())
        {
            IDbContextFactory<TestApplicationDbContext> dbcontext = scope.ServiceProvider.GetRequiredService<IDbContextFactory<TestApplicationDbContext>>();
            using (IRepository<TestApplicationDbContext> repo = dbcontext.CreateRepository())
            {
                TestUserEntity userEntity = new();
                userEntity.Name = "TestName";
                userEntity.Surname = "TestSurname";

                repo.Insert(userEntity);
                ((IRepository)repo).SaveChanges();

                userEntity.CreatedAt.ShouldNotBeNull();
            }
        }
    }

    [Fact]
    public void Case_set_UpdatedAt_DateTimeOffset()
    {
        IHostBuilder host = Host.CreateDefaultBuilder().ConfigureServices((services) =>
        {
            services.AddDbContextFactory<TestApplicationDbContext>(options =>
                options.UseInMemoryDatabase("Case_set_UpdatedAt_DateTimeOffset"));
        });

        IHost b = host.Build();

        //scope1
        using (IServiceScope scope = b.Services.CreateScope())
        {
            IDbContextFactory<TestApplicationDbContext> dbcontext = scope.ServiceProvider.GetRequiredService<IDbContextFactory<TestApplicationDbContext>>();
            using (IRepository<TestApplicationDbContext> repo = dbcontext.CreateRepository())
            {
                TestUserEntity userEntity = new();
                userEntity.Name = "TestName";
                userEntity.Surname = "TestSurname";

                userEntity.CreatedAt.ShouldBeNull();
                userEntity.UpdatedAt.ShouldBeNull();

                repo.Insert(userEntity);
                ((IRepository)repo).SaveChanges();

                userEntity.CreatedAt.ShouldNotBeNull();
                userEntity.UpdatedAt.ShouldBeNull();
            }
        }

        //scope2
        using (IServiceScope scope = b.Services.CreateScope())
        {
            IDbContextFactory<TestApplicationDbContext> dbcontext = scope.ServiceProvider.GetRequiredService<IDbContextFactory<TestApplicationDbContext>>();
            using (IRepository<TestApplicationDbContext> repo = dbcontext.CreateRepository())
            {
                TestUserEntity? userEntity = repo.FirstOrDefault<TestUserEntity>(f => f.Name == "TestName" && f.Surname == "TestSurname");

                userEntity.ShouldNotBeNull();
                userEntity.CreatedAt.ShouldNotBeNull();
                userEntity.UpdatedAt.ShouldBeNull();

                repo.Update(userEntity);
                ((IRepository)repo).SaveChanges();

                userEntity.CreatedAt.ShouldNotBeNull();
                userEntity.UpdatedAt.ShouldNotBeNull();
            }
        }
    }
}