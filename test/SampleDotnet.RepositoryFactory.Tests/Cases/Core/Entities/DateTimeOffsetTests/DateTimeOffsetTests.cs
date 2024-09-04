namespace SampleDotnet.RepositoryFactory.Tests.Cases.Core.Entities.DateTimeOffsetTests;

[Collection("Shared Collection")]
public class DateTimeOffsetTests 
{
    // A container for running SQL Server in Docker for testing purposes.
    private readonly SharedContainerFixture _shared;

    // Constructor initializes the SQL container with specific configurations.
    public DateTimeOffsetTests(SharedContainerFixture fixture)
    {
        _shared = fixture;
    }

    [Fact]
    public async void Case_set_CreatedAt_DateTimeOffset()
    {
        IHostBuilder host = Host.CreateDefaultBuilder().ConfigureServices((services) =>
        {
            services.AddDbContextFactory<DateTimeOffsetDbContext>(options =>
            {
                options.UseTestSqlConnection(_shared, "Case_set_CreatedAt_DateTimeOffset");
            });

            services.AddRepositoryFactory(ServiceLifetime.Scoped);
        });

        using (IHost build = host.Build())
        {
            build.Services.EnsureDatabaseExists<DateTimeOffsetDbContext>();

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
            services.AddDbContextFactory<DateTimeOffsetDbContext>(options =>
            {
                options.UseTestSqlConnection(_shared, "Case_set_UpdatedAt_DateTimeOffset");
            });

            services.AddRepositoryFactory(ServiceLifetime.Scoped);
        });

        using (IHost build = host.Build())
        {
            build.Services.EnsureDatabaseExists<DateTimeOffsetDbContext>();

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