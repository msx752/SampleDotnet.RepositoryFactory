﻿namespace SampleDotnet.RepositoryFactory.Tests.Cases.Core.Entities.DateTimeOffsets.Tests;

[Collection("Shared Collection")]
public class DateTimeOffsetAsyncTests
{
    // A container for running SQL Server in Docker for testing purposes.
    private readonly SharedContainerFixture _shared;

    // Constructor initializes the SQL container with specific configurations.
    public DateTimeOffsetAsyncTests(SharedContainerFixture fixture)
    {
        _shared = fixture;
    }

    [Fact]
    public async Task Case_set_CreatedAt_DateTimeOffsetAsync()
    {
        IHostBuilder host = Host.CreateDefaultBuilder().ConfigureServices((services) =>
        {
            services.AddDbContextFactory<DateTimeOffsetDbContext>(options =>
                options.UseTestSqlConnection(_shared, "Case_set_CreatedAt_DateTimeOffsetAsync"));

            services.AddRepositoryFactory(ServiceLifetime.Scoped);
        });

        using (IHost build = host.Build())
        {
            build.Services.EnsureDatabaseExists<DateTimeOffsetDbContext>();

            //scope1
            using (IServiceScope scope = build.Services.CreateScope())
            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                using (IRepository<DateTimeOffsetDbContext> repo = uow.CreateRepository<DateTimeOffsetDbContext>())
                {
                    DateTimeOffsetDbEntity userEntity = new();
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
            services.AddDbContextFactory<DateTimeOffsetDbContext>(options =>
                options.UseTestSqlConnection(_shared, "Case_set_UpdatedAt_DateTimeOffsetAsync"));

            services.AddRepositoryFactory(ServiceLifetime.Scoped);
        });

        using (IHost build = host.Build())
        {
            build.Services.EnsureDatabaseExists<DateTimeOffsetDbContext>();

            //scope1
            using (IServiceScope scope = build.Services.CreateScope())
            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                using (IRepository<DateTimeOffsetDbContext> repo = uow.CreateRepository<DateTimeOffsetDbContext>())
                {
                    DateTimeOffsetDbEntity userEntity = new();
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
                using (IRepository<DateTimeOffsetDbContext> repo = uow.CreateRepository<DateTimeOffsetDbContext>())
                {
                    DateTimeOffsetDbEntity? userEntity = await repo.FirstOrDefaultAsync<DateTimeOffsetDbEntity>(f => f.Name == "TestName" && f.Surname == "TestSurname", cancellationTokenSource.Token);

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