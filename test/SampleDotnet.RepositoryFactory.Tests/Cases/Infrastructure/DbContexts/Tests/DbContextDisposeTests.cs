namespace SampleDotnet.RepositoryFactory.Tests.Cases.Infrastructure.Data.DbContextTests
{
    [Collection("Shared Collection")]
    public class DbContextDisposeTests
    {
        // A container for running SQL Server in Docker for testing purposes.
        private readonly SharedContainerFixture _shared;

        // Constructor initializes the SQL container with specific configurations.
        public DbContextDisposeTests(SharedContainerFixture fixture)
        {
            _shared = fixture;
        }

        [Fact]
        public async Task Case_DbContext_Should_Not_Throw_ObjectDisposedException1()
        {
            IHostBuilder host = Host.CreateDefaultBuilder().ConfigureServices((services) =>
            {
                services.AddDbContextFactory<DateTimeOffsetDbContext>(options =>
                    options.UseTestSqlConnection(_shared, "Case_DbContext_Should_Not_Throw_ObjectDisposedException1"));

                services.AddRepositoryFactory(ServiceLifetime.Scoped);
            });

            using (IHost build = host.Build())
            {
                build.Services.EnsureDatabaseExists<TestDisposeDbContext>();

                //request scope
                using (IServiceScope requestScope = build.Services.CreateScope())
                using (var unitOfWork = requestScope.ServiceProvider.GetRequiredService<IUnitOfWork>())
                using (var cancellationTokenSource = new CancellationTokenSource())
                using (var repository = unitOfWork.CreateRepository<TestDisposeDbContext>())
                {
                    var user1 = new TestDisposeEntity()
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

        [Fact]
        public async Task Case_Repository_Should_Not_Throw_ObjectDisposedException2()
        {
            IHostBuilder host = Host.CreateDefaultBuilder().ConfigureServices((services) =>
            {
                services.AddDbContextFactory<DateTimeOffsetDbContext>(options =>
                    options.UseTestSqlConnection(_shared, "Case_Repository_Should_Not_Throw_ObjectDisposedException2"));

                services.AddRepositoryFactory(ServiceLifetime.Scoped);
            });

            using (IHost build = host.Build())
            {
                build.Services.EnsureDatabaseExists<TestDisposeDbContext>();

                //request scope 1
                using (IServiceScope requestScope = build.Services.CreateScope())
                using (var unitOfWork = requestScope.ServiceProvider.GetRequiredService<IUnitOfWork>())
                using (var cancellationTokenSource = new CancellationTokenSource())
                {
                    using (var repository = unitOfWork.CreateRepository<TestDisposeDbContext>())
                    {
                        var user1 = new TestDisposeEntity()
                        {
                            Name = "Name000",
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
                    int counter = 0;
                    Parallel.For(0, 50, new ParallelOptions() { MaxDegreeOfParallelism = 5, CancellationToken = cancellationTokenSource.Token }, async (i) =>
                    {
                        try
                        {
                            var val = Interlocked.Increment(ref counter);
                            using (var repository = unitOfWork.CreateRepository<TestDisposeDbContext>())
                            {
                                var user = new TestDisposeEntity()
                                {
                                    Name = $"Name",
                                    Surname = "Surname",
                                };

                                await repository.InsertAsync(user);

                                await unitOfWork.SaveChangesAsync();

                                repository.Update(user);

                                user.Name = $"Name{val.ToString("000")}";

                                await unitOfWork.SaveChangesAsync();
                            }
                        }
                        catch (Exception ex)
                        {
                            throw;
                        }
                        finally
                        {
                            Interlocked.Decrement(ref counter);
                        }
                    });

                    while (Interlocked.CompareExchange(ref counter, 0, 0) != 0)
                        await Task.Delay(1).ConfigureAwait(false);

                    using (var repository = unitOfWork.CreateRepository<TestDisposeDbContext>())
                    {
                        var users = await repository.AsQueryable<TestDisposeEntity>().ToListAsync();
                        users.Count.ShouldBe(51);
                    }
                }

                //request scope 3
                using (IServiceScope requestScope = build.Services.CreateScope())
                using (var unitOfWork = requestScope.ServiceProvider.GetRequiredService<IUnitOfWork>())
                using (var cancellationTokenSource = new CancellationTokenSource())
                {
                    List<TestDisposeEntity> entities = new();

                    using (var repository = unitOfWork.CreateRepository<TestDisposeDbContext>())
                        entities = await repository.AsQueryable<TestDisposeEntity>().ToListAsync();

                    int counter = 0;
                    Parallel.For(0, entities.Count, new ParallelOptions() { MaxDegreeOfParallelism = 10, CancellationToken = cancellationTokenSource.Token }, async (i) =>
                     {
                         try
                         {
                             var val = Interlocked.Increment(ref counter);
                             using (var repository = unitOfWork.CreateRepository<TestDisposeDbContext>())
                             {
                                 var user = entities[i];
                                 repository.Delete(user);

                                 await unitOfWork.SaveChangesAsync();
                             }
                         }
                         catch (Exception ex)
                         {
                             throw;
                         }
                         finally
                         {
                             Interlocked.Decrement(ref counter);
                         }
                     });

                    while (Interlocked.CompareExchange(ref counter, 0, 0) != 0)
                        await Task.Delay(1).ConfigureAwait(false);

                    entities.Clear();
                }

                //request scope 4
                using (IServiceScope requestScope = build.Services.CreateScope())
                using (var unitOfWork = requestScope.ServiceProvider.GetRequiredService<IUnitOfWork>())
                using (var cancellationTokenSource = new CancellationTokenSource())
                {
                    using (var repository = unitOfWork.CreateRepository<TestDisposeDbContext>())
                    {
                        var users = await repository.AsQueryable<TestDisposeEntity>().ToListAsync();
                        users.Count.ShouldBe(0);
                    }
                }
            }
        }
    }
}