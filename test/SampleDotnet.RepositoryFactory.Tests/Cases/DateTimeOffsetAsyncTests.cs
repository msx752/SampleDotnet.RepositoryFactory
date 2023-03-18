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
                TestUserEntity userEntity = new TestUserEntity();
                userEntity.Name = "TestName";
                userEntity.Surname = "TestSurname";

                await repo.InsertAsync(userEntity);
                repo.SaveChanges();

                userEntity.CreatedAt.ShouldNotBeNull();
            }
        }
    }

    [Fact]
    public async Task Case_set_UpdatedAt_DateTimeOffsettAsync()
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
                TestUserEntity userEntity = new TestUserEntity();
                userEntity.Name = "TestName";
                userEntity.Surname = "TestSurname";

                userEntity.CreatedAt.ShouldBeNull();
                userEntity.UpdatedAt.ShouldBeNull();

                await repo.InsertAsync(userEntity);
                await repo.SaveChangesAsync();

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
                TestUserEntity? userEntity = await repo.FirstOrDefaultAsync<TestUserEntity>(f => f.Name == "TestName" && f.Surname == "TestSurname");

                userEntity.ShouldNotBeNull();
                userEntity.CreatedAt.ShouldNotBeNull();
                userEntity.UpdatedAt.ShouldBeNull();

                repo.Update(userEntity);
                await repo.SaveChangesAsync();

                userEntity.CreatedAt.ShouldNotBeNull();
                userEntity.UpdatedAt.ShouldNotBeNull();
            }
        }
    }
}