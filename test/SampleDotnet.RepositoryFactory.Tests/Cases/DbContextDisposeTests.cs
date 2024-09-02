using DotNet.Testcontainers.Builders;
using SampleDotnet.RepositoryFactory.Interfaces.Core;
using SampleDotnet.RepositoryFactory.Tests.Extensions;
using SampleDotnet.RepositoryFactory.Tests.TestModels.DbContexts;
using System.Runtime.Intrinsics.X86;
using Testcontainers.MsSql;

namespace SampleDotnet.RepositoryFactory.Tests.Cases
{
    public class DbContextDisposeTests : IAsyncLifetime
    {
        private readonly MsSqlContainer _sqlContainer;

        public DbContextDisposeTests()
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
        public async Task Case_DbContext_Should_Not_Throw_ObjectDisposedException1()
        {
            IHostBuilder host = Host.CreateDefaultBuilder().ConfigureServices((services) =>
            {
                services.AddDbContextFactoryWithUnitOfWork<TestApplicationDbContext>(options =>
                {
                    var cnnBuilder = new SqlConnectionStringBuilder(_sqlContainer.GetConnectionString());
                    cnnBuilder.InitialCatalog = "Case_DbContext_Should_Not_Throw_ObjectDisposedException1";
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

        [Fact]
        public async Task Case_Repository_Should_Not_Throw_ObjectDisposedException2()
        {
            IHostBuilder host = Host.CreateDefaultBuilder().ConfigureServices((services) =>
            {
                services.AddDbContextFactoryWithUnitOfWork<TestApplicationDbContext>(options =>
                {
                    var cnnBuilder = new SqlConnectionStringBuilder(_sqlContainer.GetConnectionString());
                    cnnBuilder.InitialCatalog = "Case_Repository_Should_Not_Throw_ObjectDisposedException2";
                    cnnBuilder.TrustServerCertificate = true;
                    cnnBuilder.MultipleActiveResultSets = true;
                    cnnBuilder.ConnectRetryCount = 5;
                    cnnBuilder.ConnectTimeout = TimeSpan.FromMinutes(5).Seconds;
                    options.UseSqlServer(cnnBuilder.ToString(), (opt) => opt.EnableRetryOnFailure());
                    //options.UseInMemoryDatabase("Case_Repository_Should_Not_Throw_ObjectDisposedException");
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
                    Parallel.For(0, 100, new ParallelOptions() { MaxDegreeOfParallelism = 5, CancellationToken = cancellationTokenSource.Token }, async (i) =>
                    {
                        try
                        {
                            var val = Interlocked.Increment(ref counter);
                            using (var repository = unitOfWork.CreateRepository<TestApplicationDbContext>())
                            {
                                var user = new TestUserEntity()
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

                    using (var repository = unitOfWork.CreateRepository<TestApplicationDbContext>())
                    {
                        var users = await repository.AsQueryable<TestUserEntity>().ToListAsync();
                        users.Count.ShouldBe(101);
                    }
                }

                //request scope 3
                using (IServiceScope requestScope = build.Services.CreateScope())
                using (var unitOfWork = requestScope.ServiceProvider.GetRequiredService<IUnitOfWork>())
                using (var cancellationTokenSource = new CancellationTokenSource())
                {
                    List<TestUserEntity> entities = new();

                    using (var repository = unitOfWork.CreateRepository<TestApplicationDbContext>())
                        entities = await repository.AsQueryable<TestUserEntity>().ToListAsync();
                    
                    int counter = 0;
                    Parallel.For(0, entities.Count, new ParallelOptions() { MaxDegreeOfParallelism = 10, CancellationToken = cancellationTokenSource.Token }, async (i) =>
                     {
                         try
                         {
                             var val = Interlocked.Increment(ref counter);
                             using (var repository = unitOfWork.CreateRepository<TestApplicationDbContext>())
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
                    using (var repository = unitOfWork.CreateRepository<TestApplicationDbContext>())
                    {
                        var users = await repository.AsQueryable<TestUserEntity>().ToListAsync();
                        users.Count.ShouldBe(0);
                    }
                }
            }
        }
    }
}