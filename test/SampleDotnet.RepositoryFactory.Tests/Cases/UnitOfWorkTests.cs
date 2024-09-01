using DotNet.Testcontainers.Builders;
using SampleDotnet.RepositoryFactory.Tests.TestModels.DbContexts;
using Testcontainers.MsSql;

namespace SampleDotnet.RepositoryFactory.Tests.Cases;

// Unit tests for UnitOfWork implementation using an asynchronous approach.
public partial class UnitOfWorkTests : IAsyncLifetime
{
    // A container for running SQL Server in Docker for testing purposes.
    private readonly MsSqlContainer _sqlContainer;

    // Constructor initializes the SQL container with specific configurations.
    public UnitOfWorkTests()
    {
        _sqlContainer = new MsSqlBuilder()
            .WithPassword("Admin123!")  // Set the password for the SQL Server.
            .WithCleanUp(true)        // automatically clean up the container.
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(1433))  // Wait strategy to ensure SQL Server is ready.
            .Build();  // Build the container.
    }

    // DisposeAsync stops and disposes of the SQL container asynchronously after each test.
    public async Task DisposeAsync()
    {
        await _sqlContainer.StopAsync();  // Stop the SQL Server container.
        await _sqlContainer.DisposeAsync();  // Dispose of the SQL Server container.
    }

    // InitializeAsync starts the SQL container asynchronously before each test.
    public async Task InitializeAsync()
    {
        await _sqlContainer.StartAsync();  // Start the SQL Server container.
    }

    /// <summary>
    /// Tests the commit and rollback functionality of the UnitOfWork across two different DbContext instances.
    /// Verifies that a rollback occurs when an invalid entity is inserted, ensuring data consistency.
    /// </summary>
    [Fact]
    public async Task Case_UnitOfWork_CommitAndRollback()
    {
        // Create an IHostBuilder and configure services to use two different DbContexts with UnitOfWork.
        IHostBuilder host = Host.CreateDefaultBuilder().ConfigureServices((services) =>
        {
            // Configure FirstDbContext with SQL Server settings.
            services.AddDbContextFactoryWithUnitOfWork<FirstDbContext>(options =>
            {
                var cnnBuilder = new SqlConnectionStringBuilder(_sqlContainer.GetConnectionString());
                cnnBuilder.InitialCatalog = "FirstDbContext_Case_UnitOfWork_CommitAndRollback";  // Set the initial catalog (database name).
                cnnBuilder.TrustServerCertificate = true;  // Trust the server certificate.
                cnnBuilder.MultipleActiveResultSets = true;  // Allow multiple active result sets.
                cnnBuilder.ConnectRetryCount = 5;  // Set the number of retry attempts for connection.
                cnnBuilder.ConnectTimeout = TimeSpan.FromMinutes(5).Seconds;  // Set connection timeout.
                options.UseSqlServer(cnnBuilder.ToString(), (opt) => opt.EnableRetryOnFailure());  // Use SQL Server with retry on failure.
                options.EnableSensitiveDataLogging();  // Enable logging of sensitive data (for debugging purposes).
                options.EnableDetailedErrors();  // Enable detailed error messages (for debugging purposes).
            });

            // Configure SecondDbContext with SQL Server settings.
            services.AddDbContextFactoryWithUnitOfWork<SecondDbContext>(options =>
            {
                var cnnBuilder = new SqlConnectionStringBuilder(_sqlContainer.GetConnectionString());
                cnnBuilder.InitialCatalog = "SecondDbContext_Case_UnitOfWork_CommitAndRollback";  // Set the initial catalog (database name).
                cnnBuilder.TrustServerCertificate = true;  // Trust the server certificate.
                cnnBuilder.MultipleActiveResultSets = true;  // Allow multiple active result sets.
                cnnBuilder.ConnectRetryCount = 5;  // Set the number of retry attempts for connection.
                cnnBuilder.ConnectTimeout = TimeSpan.FromMinutes(5).Seconds;  // Set connection timeout.
                options.UseSqlServer(cnnBuilder.ToString(), (opt) => opt.EnableRetryOnFailure());  // Use SQL Server with retry on failure.
                options.EnableSensitiveDataLogging();  // Enable logging of sensitive data (for debugging purposes).
                options.EnableDetailedErrors();  // Enable detailed error messages (for debugging purposes).
            });
        });

        // Build the IHost and get the required services for testing.
        using (IHost build = host.Build())
        {
            // Ensure FirstDbContext database is created and clean up any existing data.
            var firstDbContextFactory = build.Services.GetRequiredService<IDbContextFactory<FirstDbContext>>();
            using (var context = firstDbContextFactory.CreateDbContext())
            {
                context.Database.EnsureCreated();  // Ensure the database for FirstDbContext is created.
                await context.CLEAN_TABLES_DO_NOT_USE_PRODUCTION();  // Clean up table records in FirstDbContext.
            }

            // Ensure SecondDbContext database is created and clean up any existing data.
            var secondDbContextFactory = build.Services.GetRequiredService<IDbContextFactory<SecondDbContext>>();
            using (var context = secondDbContextFactory.CreateDbContext())
            {
                context.Database.EnsureCreated();  // Ensure the database for SecondDbContext is created.
                await context.CLEAN_TABLES_DO_NOT_USE_PRODUCTION();  // Clean up table records in SecondDbContext.
            }

            // Begin a new request scope for dependency injection.
            using (IServiceScope requestScope = build.Services.CreateScope())
            using (var unitOfWork = requestScope.ServiceProvider.GetRequiredService<IUnitOfWork>())
            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                // Instance 1: Insert a valid entity into FirstDbContext.
                {
                    using (IRepository<FirstDbContext> repo = unitOfWork.CreateRepository<FirstDbContext>())
                    {
                        FirstDbEntity entity = new();  // Create a new entity for FirstDbContext.
                        entity.ProductName = "Product1";  // Set the product name.
                        entity.Price = 10.5M;  // Set the price.

                        entity.CreatedAt.ShouldBeNull();  // Ensure CreatedAt is null before insert.
                        entity.UpdatedAt.ShouldBeNull();  // Ensure UpdatedAt is null before insert.

                        await repo.InsertAsync(entity, cancellationTokenSource.Token);  // Insert the entity asynchronously.

                        entity.CreatedAt.ShouldNotBeNull();  // CreatedAt should be set after insert.
                        entity.UpdatedAt.ShouldBeNull();  // UpdatedAt should still be null after insert.
                    }
                }

                // Instance 2: Insert a valid entity into SecondDbContext.
                {
                    using (IRepository<SecondDbContext> repo = unitOfWork.CreateRepository<SecondDbContext>())
                    {
                        SecondDbEntity entity = new();  // Create a new entity for SecondDbContext.
                        entity.CustomerName = "Customer1";  // Set the customer name.

                        entity.CreatedAt.ShouldBeNull();  // Ensure CreatedAt is null before insert.
                        entity.UpdatedAt.ShouldBeNull();  // Ensure UpdatedAt is null before insert.

                        await repo.InsertAsync(entity, cancellationTokenSource.Token);  // Insert the entity asynchronously.

                        entity.CreatedAt.ShouldNotBeNull();  // CreatedAt should be set after insert.
                        entity.UpdatedAt.ShouldBeNull();  // UpdatedAt should still be null after insert.
                    }
                }

                // Commit the changes for both DbContexts.
                var isSucceed = await unitOfWork.SaveChangesAsync();  // Save changes and commit.
                isSucceed.ShouldBeTrue();  // Assert that the commit was successful.

                // Instance 3: Select and update the committed entity in FirstDbContext.
                {
                    using (IRepository<FirstDbContext> repo = unitOfWork.CreateRepository<FirstDbContext>())
                    {
                        var entity = await repo.FirstOrDefaultAsync<FirstDbEntity>(f => f.ProductName == "Product1");  // Fetch the entity.
                        entity.ShouldNotBeNull();  // Ensure the entity is found.

                        entity.ProductName = "Product1Updated";  // Update the product name.

                        repo.Update(entity);  // Update the entity in the repository.

                        entity.CreatedAt.ShouldNotBeNull();  // Ensure CreatedAt is still set.
                        entity.UpdatedAt.ShouldNotBeNull();  // UpdatedAt should now be set after update.
                    }
                }

                // Instance 4: Try to insert an invalid entity into SecondDbContext.
                {
                    using (IRepository<SecondDbContext> repo = unitOfWork.CreateRepository<SecondDbContext>())
                    {
                        SecondDbEntity entity = new();  // Create a new entity for SecondDbContext.
                        entity.CustomerName = null;  // Set CustomerName to null, which is invalid.

                        entity.CreatedAt.ShouldBeNull();  // Ensure CreatedAt is null before insert.
                        entity.UpdatedAt.ShouldBeNull();  // Ensure UpdatedAt is null before insert.

                        await repo.InsertAsync(entity, cancellationTokenSource.Token);  // Insert the entity asynchronously.

                        entity.CreatedAt.ShouldNotBeNull();  // CreatedAt should be set after insert.
                        entity.UpdatedAt.ShouldBeNull();  // UpdatedAt should still be null after insert.
                    }
                }

                // Instance 5: Select and delete the committed entity in SecondDbContext.
                {
                    using (IRepository<SecondDbContext> repo = unitOfWork.CreateRepository<SecondDbContext>())
                    {
                        var entity = await repo.FirstOrDefaultAsync<SecondDbEntity>(f => f.CustomerName == "Customer1");  // Fetch the entity.
                        entity.ShouldNotBeNull();  // Ensure the entity is found.

                        repo.Delete(entity);  // Delete the entity from the repository.
                    }
                }

                // Attempt to save changes, expecting a DbUpdateException due to the invalid entity in SecondDbContext.
                await Assert.ThrowsAsync<DbUpdateException>(async () =>
                    await unitOfWork.SaveChangesAsync(cancellationTokenSource.Token));

                // Ensure exception details are set correctly.
                unitOfWork.SaveChangesException.ShouldNotBeNull();  // SaveChangesException should not be null.
                unitOfWork.IsDbConcurrencyExceptionThrown.ShouldBeFalse();  // DbConcurrencyException should not be thrown.

                // Instance 6: Verify the entity in FirstDbContext after rollback.
                {
                    using (IRepository<FirstDbContext> repo = unitOfWork.CreateRepository<FirstDbContext>())
                    {
                        var entity = await repo.FirstOrDefaultAsync<FirstDbEntity>(f => f.ProductName == "Product1");  // Attempt to find the entity.
                        entity.ShouldNotBeNull();  // Entity should still exist since rollback should only affect new changes.
                    }
                }

                // Instance 7: Verify the entity in SecondDbContext after rollback.
                {
                    using (IRepository<SecondDbContext> repo = unitOfWork.CreateRepository<SecondDbContext>())
                    {
                        var entity = await repo.FirstOrDefaultAsync<SecondDbEntity>(f => f.CustomerName == "Customer1");  // Attempt to find the entity.
                        entity.ShouldNotBeNull();  // Entity should still exist since rollback should only affect new changes.
                    }
                }

                // Instance 8: Try to select discarded entity changes in FirstDbContext.
                {
                    using (IRepository<FirstDbContext> repo = unitOfWork.CreateRepository<FirstDbContext>())
                    {
                        var entity = await repo.FirstOrDefaultAsync<FirstDbEntity>(f => f.ProductName == "Product1Updated");  // Attempt to find the updated entity.
                        entity.ShouldBeNull();  // Entity should not be found as changes were rolled back.
                    }
                }

                // Instance 9: Try to select discarded entity changes in SecondDbContext.
                {
                    using (IRepository<SecondDbContext> repo = unitOfWork.CreateRepository<SecondDbContext>())
                    {
                        var entity = await repo.FirstOrDefaultAsync<SecondDbEntity>(f => f.CustomerName == null);  // Attempt to find the invalid entity.
                        entity.ShouldBeNull();  // Entity should not be found as changes were rolled back.
                    }
                }

                // Attempt to commit again, expecting no changes to be committed, but operation should succeed.
                await unitOfWork.SaveChangesAsync(cancellationTokenSource.Token);  // Save changes again to ensure rollback handled correctly.
            }
        }
    }

    /// <summary>
    /// Tests that the UnitOfWork does not skip detecting changes in the DbContext.
    /// Ensures that all entity changes are correctly tracked and persisted.
    /// </summary>
    [Fact]
    public async Task Case_UnitOfWork_Do_Not_Skip_DetectChanges()
    {
        // Create an IHostBuilder and configure services to use TestApplicationDbContext with UnitOfWork.
        IHostBuilder host = Host.CreateDefaultBuilder().ConfigureServices((services) =>
        {
            // Configure TestApplicationDbContext with SQL Server settings.
            services.AddDbContextFactoryWithUnitOfWork<TestApplicationDbContext>(options =>
            {
                var cnnBuilder = new SqlConnectionStringBuilder(_sqlContainer.GetConnectionString());
                cnnBuilder.InitialCatalog = "Case_UnitOfWork_Do_Not_Skip_DetectChanges";  // Set the initial catalog (database name).
                cnnBuilder.TrustServerCertificate = true;  // Trust the server certificate.
                cnnBuilder.MultipleActiveResultSets = true;  // Allow multiple active result sets.
                cnnBuilder.ConnectRetryCount = 5;  // Set the number of retry attempts for connection.
                cnnBuilder.ConnectTimeout = TimeSpan.FromMinutes(5).Seconds;  // Set connection timeout.
                options.UseSqlServer(cnnBuilder.ToString(), (opt) => opt.EnableRetryOnFailure());  // Use SQL Server with retry on failure.
                options.EnableSensitiveDataLogging();  // Enable logging of sensitive data (for debugging purposes).
                options.EnableDetailedErrors();  // Enable detailed error messages (for debugging purposes).
            });
        });

        // Build the IHost and get the required services for testing.
        using (IHost build = host.Build())
        {
            // Ensure TestApplicationDbContext database is created and clean up any existing data.
            var firstDbContextFactory = build.Services.GetRequiredService<IDbContextFactory<TestApplicationDbContext>>();
            using (var context = firstDbContextFactory.CreateDbContext())
            {
                context.Database.EnsureCreated();  // Ensure the database for TestApplicationDbContext is created.
                await context.CLEAN_TABLES_DO_NOT_USE_PRODUCTION();  // Clean up table records in TestApplicationDbContext.
            }

            // Begin a new request scope for dependency injection.
            using (IServiceScope requestScope = build.Services.CreateScope())
            using (var unitOfWork = requestScope.ServiceProvider.GetRequiredService<IUnitOfWork>())
            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                // First part: Insert a new entity and save changes to the database.
                using (var repository = unitOfWork.CreateRepository<TestApplicationDbContext>())
                {
                    var user1 = new TestUserEntity()
                    {
                        Name = "Name",  // Set initial name.
                        Surname = "Surname",  // Set initial surname.
                    };

                    user1.CreatedAt.ShouldBeNull();  // Ensure CreatedAt is null before insert.
                    await repository.InsertAsync(user1);  // Insert the entity asynchronously.

                    await unitOfWork.SaveChangesAsync();  // Save changes to the database.
                }

                // Second part: Retrieve the entity, update it, and ensure changes are detected.
                using (var repository = unitOfWork.CreateRepository<TestApplicationDbContext>())
                {
                    var user1 = await repository.FirstOrDefaultAsync<TestUserEntity>(f => f.Surname == "Surname");  // Fetch the entity by surname.

                    user1.ShouldNotBeNull();  // Ensure the entity is found.
                    user1.CreatedAt.ShouldNotBeNull();  // Ensure CreatedAt is set after insertion.

                    await unitOfWork.SaveChangesAsync();  // Save changes to the database (no changes, but ensure no errors).

                    user1.Name = "NameUpdated2";  // Update the entity's name.

                    await unitOfWork.SaveChangesAsync();  // Save changes again to update the entity in the database.

                    user1.CreatedAt.ShouldNotBeNull();  // Ensure CreatedAt is still set after update.

                    var user2 = await repository.FirstOrDefaultAsync<TestUserEntity>(f => f.Name == "NameUpdated2");  // Fetch the entity by updated name.
                    user2.ShouldNotBeNull();  // Ensure the updated entity is found.
                }
            }
        }
    }

    /// <summary>
    /// Validates the rollback functionality in the UnitOfWork pattern.
    /// Ensures that no changes are committed when an update operation fails due to invalid data.
    /// </summary>
    [Fact]
    public async Task Case_UnitOfWork_Rollback()
    {
        // Setup an IHostBuilder to configure services for the test.
        IHostBuilder host = Host.CreateDefaultBuilder().ConfigureServices((services) =>
        {
            // Configure DbContextFactory and UnitOfWork for the test.
            services.AddDbContextFactoryWithUnitOfWork<TestApplicationDbContext>(options =>
            {
                var cnnBuilder = new SqlConnectionStringBuilder(_sqlContainer.GetConnectionString());
                cnnBuilder.InitialCatalog = "Case_UnitOfWork_Rollback";  // Set the database name.
                cnnBuilder.TrustServerCertificate = true;  // Trust the SQL Server certificate.
                cnnBuilder.MultipleActiveResultSets = true;  // Enable multiple active result sets.
                cnnBuilder.ConnectRetryCount = 5;  // Set connection retry count.
                cnnBuilder.ConnectTimeout = TimeSpan.FromMinutes(5).Seconds;  // Set connection timeout.
                options.UseSqlServer(cnnBuilder.ToString(), (opt) => opt.EnableRetryOnFailure());  // Use SQL Server with retry on failure enabled.
                options.EnableSensitiveDataLogging();  // Enable sensitive data logging.
                options.EnableDetailedErrors();  // Enable detailed error messages.
            });
        });

        using (IHost build = host.Build())
        {
            var testApplicationDbContextFactory = build.Services.GetRequiredService<IDbContextFactory<TestApplicationDbContext>>();
            using (var context = testApplicationDbContextFactory.CreateDbContext())
            {
                context.Database.EnsureCreated();  // Ensure the database is created.
                await context.CLEAN_TABLES_DO_NOT_USE_PRODUCTION();  // Clean up any existing data.
            }

            // Begin a new request scope for dependency injection.
            using (IServiceScope requestScope = build.Services.CreateScope())
            using (var unitOfWork = requestScope.ServiceProvider.GetRequiredService<IUnitOfWork>())
            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                TestUserEntity createdEntityFromFirstInstance = null;

                // First instance: Insert a valid entity into the database.
                {
                    using (IRepository<TestApplicationDbContext> repo = unitOfWork.CreateRepository<TestApplicationDbContext>())
                    {
                        TestUserEntity entity = new();  // Create a new entity.
                        entity.Name = "TestName";  // Set entity name.
                        entity.Surname = "TestSurname";  // Set entity surname.

                        entity.CreatedAt.ShouldBeNull();  // Ensure CreatedAt is null before insert.
                        entity.UpdatedAt.ShouldBeNull();  // Ensure UpdatedAt is null before insert.

                        await repo.InsertAsync(entity, cancellationTokenSource.Token);  // Insert entity asynchronously.

                        entity.CreatedAt.ShouldNotBeNull();  // CreatedAt should be set after insert.
                        entity.UpdatedAt.ShouldBeNull();  // UpdatedAt should still be null after insert.

                        createdEntityFromFirstInstance = entity;  // Store reference for later use.
                    }
                }

                // Second instance: Try to select the non-committed entity.
                {
                    using (IRepository<TestApplicationDbContext> repo = unitOfWork.CreateRepository<TestApplicationDbContext>())
                    {
                        TestUserEntity? entity = await repo.FirstOrDefaultAsync<TestUserEntity>(f => f.Name == "TestName" && f.Surname == "TestSurname", cancellationTokenSource.Token);

                        entity.ShouldBeNull();  // Entity should not be found since the transaction is not committed.
                    }
                }

                // Third instance: Try to update the entity's required field to null, tracked by a different DbContext instance.
                {
                    using (IRepository<TestApplicationDbContext> repo = unitOfWork.CreateRepository<TestApplicationDbContext>())
                    {
                        TestUserEntity? entity = await repo.FirstOrDefaultAsync<TestUserEntity>(f => f.Name == "TestName", cancellationTokenSource.Token);
                        entity.ShouldBeNull();  // Entity should not be found in this context.

                        createdEntityFromFirstInstance.Name = null;  // Set name to null, which is invalid.

                        repo.Update(createdEntityFromFirstInstance);  // Update entity with invalid data.

                        createdEntityFromFirstInstance.CreatedAt.ShouldNotBeNull();  // Ensure CreatedAt is still set.
                        createdEntityFromFirstInstance.UpdatedAt.ShouldNotBeNull();  // UpdatedAt should now be set.
                    }
                }

                // Attempt to save changes, expecting a DbUpdateException due to invalid data.
                await Assert.ThrowsAsync<DbUpdateException>(async () =>
                  await unitOfWork.SaveChangesAsync(cancellationTokenSource.Token));

                // Ensure exception details are set correctly.
                unitOfWork.SaveChangesException.ShouldNotBeNull();
                unitOfWork.IsDbConcurrencyExceptionThrown.ShouldBeFalse();

                // Fourth instance: Ensure the entity state is reverted back after rollback.
                {
                    using (IRepository<TestApplicationDbContext> repo = unitOfWork.CreateRepository<TestApplicationDbContext>())
                    {
                        TestUserEntity? entity = await repo.FirstOrDefaultAsync<TestUserEntity>(f => f.Name == "TestName", cancellationTokenSource.Token);
                        entity.ShouldBeNull();  // Entity should be null since changes were rolled back.

                        entity = await repo.FirstOrDefaultAsync<TestUserEntity>(f => f.Name == null, cancellationTokenSource.Token);
                        entity.ShouldBeNull();  // Entity with null name should not exist.

                        var entities = repo.AsQueryable<TestUserEntity>().ToList();
                        entities.Count.ShouldBeEquivalentTo(0);  // Ensure no entities are present.
                    }
                }
            }
        }
    }

    /// <summary>
    /// Tests the rollback mechanism when working with two different DbContext instances.
    /// Verifies that all changes are rolled back if an invalid operation is performed in any DbContext.
    /// </summary>
    [Fact]
    public async Task Case_UnitOfWork_TwoDifferent_DbContext_Rollback()
    {
        // Create an IHostBuilder and configure services to use two different DbContexts with UnitOfWork.
        IHostBuilder host = Host.CreateDefaultBuilder().ConfigureServices((services) =>
        {
            // Configure FirstDbContext with SQL Server settings.
            services.AddDbContextFactoryWithUnitOfWork<FirstDbContext>(options =>
            {
                var cnnBuilder = new SqlConnectionStringBuilder(_sqlContainer.GetConnectionString());
                cnnBuilder.InitialCatalog = "TwoDifferent_DbContext_Rollback_FirstDb";  // Set the initial catalog (database name).
                cnnBuilder.TrustServerCertificate = true;  // Trust the server certificate.
                cnnBuilder.MultipleActiveResultSets = true;  // Allow multiple active result sets.
                cnnBuilder.ConnectRetryCount = 5;  // Set the number of retry attempts for connection.
                cnnBuilder.ConnectTimeout = TimeSpan.FromMinutes(5).Seconds;  // Set connection timeout.
                options.UseSqlServer(cnnBuilder.ToString(), (opt) => opt.EnableRetryOnFailure());  // Use SQL Server with retry on failure.
                options.EnableSensitiveDataLogging();  // Enable logging of sensitive data (for debugging purposes).
                options.EnableDetailedErrors();  // Enable detailed error messages (for debugging purposes).
            });

            // Configure SecondDbContext with SQL Server settings.
            services.AddDbContextFactoryWithUnitOfWork<SecondDbContext>(options =>
            {
                var cnnBuilder = new SqlConnectionStringBuilder(_sqlContainer.GetConnectionString());
                cnnBuilder.InitialCatalog = "TwoDifferent_DbContext_Rollback_SecondDb";  // Set the initial catalog (database name).
                cnnBuilder.TrustServerCertificate = true;  // Trust the server certificate.
                cnnBuilder.MultipleActiveResultSets = true;  // Allow multiple active result sets.
                cnnBuilder.ConnectRetryCount = 5;  // Set the number of retry attempts for connection.
                cnnBuilder.ConnectTimeout = TimeSpan.FromMinutes(5).Seconds;  // Set connection timeout.
                options.UseSqlServer(cnnBuilder.ToString(), (opt) => opt.EnableRetryOnFailure());  // Use SQL Server with retry on failure.
                options.EnableSensitiveDataLogging();  // Enable logging of sensitive data (for debugging purposes).
                options.EnableDetailedErrors();  // Enable detailed error messages (for debugging purposes).
            });
        });

        // Build the IHost and get the required services for testing.
        using (IHost build = host.Build())
        {
            // Ensure FirstDbContext database is created and clean up any existing data.
            var firstDbContextFactory = build.Services.GetRequiredService<IDbContextFactory<FirstDbContext>>();
            using (var context = firstDbContextFactory.CreateDbContext())
            {
                context.Database.EnsureCreated();  // Ensure the database for FirstDbContext is created.
                await context.CLEAN_TABLES_DO_NOT_USE_PRODUCTION();  // Clean up table records in FirstDbContext.
            }

            // Ensure SecondDbContext database is created and clean up any existing data.
            var secondDbContextFactory = build.Services.GetRequiredService<IDbContextFactory<SecondDbContext>>();
            using (var context = secondDbContextFactory.CreateDbContext())
            {
                context.Database.EnsureCreated();  // Ensure the database for SecondDbContext is created.
                await context.CLEAN_TABLES_DO_NOT_USE_PRODUCTION();  // Clean up table records in SecondDbContext.
            }

            // Begin a new request scope for dependency injection.
            using (IServiceScope requestScope = build.Services.CreateScope())
            using (var unitOfWork = requestScope.ServiceProvider.GetRequiredService<IUnitOfWork>())
            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                // Instance 1: Insert a valid entity into FirstDbContext.
                {
                    using (IRepository<FirstDbContext> repo = unitOfWork.CreateRepository<FirstDbContext>())
                    {
                        FirstDbEntity entity = new();  // Create a new entity for FirstDbContext.
                        entity.ProductName = "Product1";  // Set the product name.
                        entity.Price = 10.5M;  // Set the price.

                        entity.CreatedAt.ShouldBeNull();  // Ensure CreatedAt is null before insert.
                        entity.UpdatedAt.ShouldBeNull();  // Ensure UpdatedAt is null before insert.

                        await repo.InsertAsync(entity, cancellationTokenSource.Token);  // Insert the entity asynchronously.

                        entity.CreatedAt.ShouldNotBeNull();  // CreatedAt should be set after insert.
                        entity.UpdatedAt.ShouldBeNull();  // UpdatedAt should still be null after insert.
                    }
                }

                // Instance 2: Insert an invalid entity into SecondDbContext.
                {
                    using (IRepository<SecondDbContext> repo = unitOfWork.CreateRepository<SecondDbContext>())
                    {
                        SecondDbEntity entity = new();  // Create a new entity for SecondDbContext.
                        entity.CustomerName = null;  // Set CustomerName to null, which is invalid.

                        entity.CreatedAt.ShouldBeNull();  // Ensure CreatedAt is null before insert.
                        entity.UpdatedAt.ShouldBeNull();  // Ensure UpdatedAt is null before insert.

                        await repo.InsertAsync(entity, cancellationTokenSource.Token);  // Insert the entity asynchronously.

                        entity.CreatedAt.ShouldNotBeNull();  // CreatedAt should be set after insert.
                        entity.UpdatedAt.ShouldBeNull();  // UpdatedAt should still be null after insert.
                    }
                }

                // Attempt to save changes, expecting a DbUpdateException due to the invalid entity in SecondDbContext.
                await Assert.ThrowsAsync<DbUpdateException>(async () =>
                    await unitOfWork.SaveChangesAsync(cancellationTokenSource.Token));

                // Ensure exception details are set correctly.
                unitOfWork.SaveChangesException.ShouldNotBeNull();  // SaveChangesException should not be null.
                unitOfWork.IsDbConcurrencyExceptionThrown.ShouldBeFalse();  // DbConcurrencyException should not be thrown.

                // Instance 4: Select reverted back entity from FirstDbContext after rollback.
                {
                    using (IRepository<FirstDbContext> repo = unitOfWork.CreateRepository<FirstDbContext>())
                    {
                        var entity = await repo.FirstOrDefaultAsync<FirstDbEntity>(f => f.ProductName == "Product1");  // Attempt to find the entity.
                        entity.ShouldBeNull();  // Entity should be null since changes were rolled back.

                        var entities = repo.AsQueryable<FirstDbEntity>().ToList();  // Get all entities in FirstDbContext.
                        entities.Count.ShouldBeEquivalentTo(0);  // Ensure no entities are present after rollback.
                    }
                }

                // Instance 5: Select reverted back entity from SecondDbContext after rollback.
                {
                    using (IRepository<SecondDbContext> repo = unitOfWork.CreateRepository<SecondDbContext>())
                    {
                        var entity = await repo.FirstOrDefaultAsync<SecondDbEntity>(f => f.CustomerName == null);  // Attempt to find the entity.
                        entity.ShouldBeNull();  // Entity should be null since changes were rolled back.

                        var entities = repo.AsQueryable<SecondDbEntity>().ToList();  // Get all entities in SecondDbContext.
                        entities.Count.ShouldBeEquivalentTo(0);  // Ensure no entities are present after rollback.
                    }
                }
            }
        }
    }

    /// <summary>
    /// Tests partial commits and rollbacks in the UnitOfWork.
    /// Verifies that changes before a save point are committed, while subsequent invalid changes are rolled back.
    /// </summary>
    [Fact]
    public async Task Case_UnitOfWork_PartialCommitAndRollback()
    {
        /*
            Based on the analysis of existing tests, here is a suggested new test scenario:

            Test Case: Simultaneous Operations with Save Points
            Purpose: Test the ability of the Unit of Work to handle save points and partial rollbacks.
                This scenario would simulate a situation where some operations need to be committed while others need to be rolled back within the same Unit of Work.
:
            Setup:
                Insert a valid entity into FirstDbContext.
                Insert a valid entity into SecondDbContext.

            Commit Save Point:
                Commit the current state to create a save point. This would simulate the scenario where we want some operations to be committed and others to be rolled back.

            Insert and Rollback:
                Insert an invalid entity into FirstDbContext.
                Insert an invalid entity into SecondDbContext.
                Attempt to save changes, expecting a DbUpdateException.

            Verify Partial Commit:
                Verify that the entities inserted before the save point are committed and present in the database.
                Verify that entities inserted after the save point are rolled back and do not exist in the database.
        */

        // Create an IHostBuilder and configure services for testing.
        IHostBuilder host = Host.CreateDefaultBuilder().ConfigureServices((services) =>
        {
            services.AddDbContextFactoryWithUnitOfWork<FirstDbContext>(options =>
            {
                var cnnBuilder = new SqlConnectionStringBuilder(_sqlContainer.GetConnectionString());
                cnnBuilder.InitialCatalog = "PartialCommitAndRollback_FirstDb";  // Set the initial catalog.
                cnnBuilder.TrustServerCertificate = true;
                cnnBuilder.MultipleActiveResultSets = true;
                cnnBuilder.ConnectRetryCount = 5;
                cnnBuilder.ConnectTimeout = TimeSpan.FromMinutes(5).Seconds;
                options.UseSqlServer(cnnBuilder.ToString(), (opt) => opt.EnableRetryOnFailure());
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            });

            services.AddDbContextFactoryWithUnitOfWork<SecondDbContext>(options =>
            {
                var cnnBuilder = new SqlConnectionStringBuilder(_sqlContainer.GetConnectionString());
                cnnBuilder.InitialCatalog = "PartialCommitAndRollback_SecondDb";  // Set the initial catalog.
                cnnBuilder.TrustServerCertificate = true;
                cnnBuilder.MultipleActiveResultSets = true;
                cnnBuilder.ConnectRetryCount = 5;
                cnnBuilder.ConnectTimeout = TimeSpan.FromMinutes(5).Seconds;
                options.UseSqlServer(cnnBuilder.ToString(), (opt) => opt.EnableRetryOnFailure());
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            });
        });

        using (IHost build = host.Build())
        {
            var firstDbContextFactory = build.Services.GetRequiredService<IDbContextFactory<FirstDbContext>>();
            using (var context = firstDbContextFactory.CreateDbContext())
            {
                context.Database.EnsureCreated();
                await context.CLEAN_TABLES_DO_NOT_USE_PRODUCTION();
            }

            var secondDbContextFactory = build.Services.GetRequiredService<IDbContextFactory<SecondDbContext>>();
            using (var context = secondDbContextFactory.CreateDbContext())
            {
                context.Database.EnsureCreated();
                await context.CLEAN_TABLES_DO_NOT_USE_PRODUCTION();
            }

            using (IServiceScope requestScope = build.Services.CreateScope())
            using (var unitOfWork = requestScope.ServiceProvider.GetRequiredService<IUnitOfWork>())
            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                // Step 1: Insert valid entities into both DbContexts.
                {
                    using (IRepository<FirstDbContext> repo = unitOfWork.CreateRepository<FirstDbContext>())
                    {
                        var entity = new FirstDbEntity { ProductName = "ValidProduct", Price = 100M };
                        await repo.InsertAsync(entity, cancellationTokenSource.Token);
                    }

                    using (IRepository<SecondDbContext> repo = unitOfWork.CreateRepository<SecondDbContext>())
                    {
                        var entity = new SecondDbEntity { CustomerName = "ValidCustomer" };
                        await repo.InsertAsync(entity, cancellationTokenSource.Token);
                    }

                    await unitOfWork.SaveChangesAsync(cancellationTokenSource.Token);  // Commit to create a save point.
                }

                // Step 2: Insert invalid entities into both DbContexts, then attempt to rollback.
                {
                    using (IRepository<FirstDbContext> repo = unitOfWork.CreateRepository<FirstDbContext>())
                    {
                        var entity = new FirstDbEntity { ProductName = null, Price = 50M };  // Invalid entity.
                        await repo.InsertAsync(entity, cancellationTokenSource.Token);
                    }

                    using (IRepository<SecondDbContext> repo = unitOfWork.CreateRepository<SecondDbContext>())
                    {
                        var entity = new SecondDbEntity { CustomerName = null };  // Invalid entity.
                        await repo.InsertAsync(entity, cancellationTokenSource.Token);
                    }

                    // This should throw due to invalid entities.
                    await Assert.ThrowsAsync<DbUpdateException>(async () => await unitOfWork.SaveChangesAsync(cancellationTokenSource.Token));
                }

                // Step 3: Verify the first set of entities were committed, second set was rolled back.
                {
                    using (IRepository<FirstDbContext> repo = unitOfWork.CreateRepository<FirstDbContext>())
                    {
                        var validEntity = await repo.FirstOrDefaultAsync<FirstDbEntity>(f => f.ProductName == "ValidProduct");
                        validEntity.ShouldNotBeNull();  // Ensure the valid entity exists.

                        var invalidEntity = await repo.FirstOrDefaultAsync<FirstDbEntity>(f => f.ProductName == null);
                        invalidEntity.ShouldBeNull();  // Ensure the invalid entity was rolled back.
                    }

                    using (IRepository<SecondDbContext> repo = unitOfWork.CreateRepository<SecondDbContext>())
                    {
                        var validEntity = await repo.FirstOrDefaultAsync<SecondDbEntity>(f => f.CustomerName == "ValidCustomer");
                        validEntity.ShouldNotBeNull();  // Ensure the valid entity exists.

                        var invalidEntity = await repo.FirstOrDefaultAsync<SecondDbEntity>(f => f.CustomerName == null);
                        invalidEntity.ShouldBeNull();  // Ensure the invalid entity was rolled back.
                    }
                }
            }
        }
    }

    /// <summary>
    /// Tests the UnitOfWork's handling of interleaved valid and invalid operations.
    /// Ensures that all changes are rolled back if any operation fails, preserving data integrity.
    /// </summary>
    [Fact]
    public async Task Case_UnitOfWork_InterleavedValidAndInvalidOperations()
    {
        /*
            Reasoning Behind This Test Scenario
            Testing Interleaved Valid and Invalid Operations:

            The scenario tests the behavior of the UnitOfWork when it encounters a mix of valid and invalid operations in the same transaction.
            It's essential to ensure that the presence of any invalid operation triggers a complete rollback of all operations, preserving data integrity.
            Ensuring Complete Rollback:

            This test validates that the rollback mechanism is robust and correctly reverts all changes, not just those that occurred after the invalid operation.
            This is crucial for maintaining consistency across transactions, especially when multiple DbContext instances are involved.
            Simulating Real-World Scenarios:

            In real-world applications, it is common to perform multiple operations within a single transaction. These operations may depend on each other,
            and failure to properly handle an interleaved invalid operation could lead to data corruption or inconsistent states. This test ensures the application can handle such scenarios gracefully.
         */

        // Create an IHostBuilder and configure services for testing.
        IHostBuilder host = Host.CreateDefaultBuilder().ConfigureServices((services) =>
        {
            services.AddDbContextFactoryWithUnitOfWork<FirstDbContext>(options =>
            {
                var cnnBuilder = new SqlConnectionStringBuilder(_sqlContainer.GetConnectionString());
                cnnBuilder.InitialCatalog = "InterleavedOperations_FirstDb";  // Set the initial catalog.
                cnnBuilder.TrustServerCertificate = true;
                cnnBuilder.MultipleActiveResultSets = true;
                cnnBuilder.ConnectRetryCount = 5;
                cnnBuilder.ConnectTimeout = TimeSpan.FromMinutes(5).Seconds;
                options.UseSqlServer(cnnBuilder.ToString(), (opt) => opt.EnableRetryOnFailure());
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            });

            services.AddDbContextFactoryWithUnitOfWork<SecondDbContext>(options =>
            {
                var cnnBuilder = new SqlConnectionStringBuilder(_sqlContainer.GetConnectionString());
                cnnBuilder.InitialCatalog = "InterleavedOperations_SecondDb";  // Set the initial catalog.
                cnnBuilder.TrustServerCertificate = true;
                cnnBuilder.MultipleActiveResultSets = true;
                cnnBuilder.ConnectRetryCount = 5;
                cnnBuilder.ConnectTimeout = TimeSpan.FromMinutes(5).Seconds;
                options.UseSqlServer(cnnBuilder.ToString(), (opt) => opt.EnableRetryOnFailure());
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            });
        });

        using (IHost build = host.Build())
        {
            var firstDbContextFactory = build.Services.GetRequiredService<IDbContextFactory<FirstDbContext>>();
            using (var context = firstDbContextFactory.CreateDbContext())
            {
                context.Database.EnsureCreated();
                await context.CLEAN_TABLES_DO_NOT_USE_PRODUCTION();
            }

            var secondDbContextFactory = build.Services.GetRequiredService<IDbContextFactory<SecondDbContext>>();
            using (var context = secondDbContextFactory.CreateDbContext())
            {
                context.Database.EnsureCreated();
                await context.CLEAN_TABLES_DO_NOT_USE_PRODUCTION();
            }

            using (IServiceScope requestScope = build.Services.CreateScope())
            using (var unitOfWork = requestScope.ServiceProvider.GetRequiredService<IUnitOfWork>())
            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                // Step 1: Insert valid and invalid entities in an interleaved manner.
                {
                    using (IRepository<FirstDbContext> repo = unitOfWork.CreateRepository<FirstDbContext>())
                    {
                        var validEntity1 = new FirstDbEntity { ProductName = "ValidProduct1", Price = 100M };
                        await repo.InsertAsync(validEntity1, cancellationTokenSource.Token);  // Insert valid entity.

                        var invalidEntity = new FirstDbEntity { ProductName = null, Price = 50M };  // Invalid entity.
                        await repo.InsertAsync(invalidEntity, cancellationTokenSource.Token);  // Insert invalid entity.
                    }

                    using (IRepository<SecondDbContext> repo = unitOfWork.CreateRepository<SecondDbContext>())
                    {
                        var validEntity2 = new SecondDbEntity { CustomerName = "ValidCustomer2" };
                        await repo.InsertAsync(validEntity2, cancellationTokenSource.Token);  // Insert valid entity.
                    }
                }

                // Step 2: Attempt to save all changes; expect rollback due to the invalid entity.
                await Assert.ThrowsAsync<DbUpdateException>(async () => await unitOfWork.SaveChangesAsync(cancellationTokenSource.Token));

                // Step 3: Verify all changes were rolled back, including the valid ones.
                {
                    using (IRepository<FirstDbContext> repo = unitOfWork.CreateRepository<FirstDbContext>())
                    {
                        var validEntity = await repo.FirstOrDefaultAsync<FirstDbEntity>(f => f.ProductName == "ValidProduct1");
                        validEntity.ShouldBeNull();  // Ensure the valid entity was rolled back.

                        var invalidEntity = await repo.FirstOrDefaultAsync<FirstDbEntity>(f => f.ProductName == null);
                        invalidEntity.ShouldBeNull();  // Ensure the invalid entity was rolled back.
                    }

                    using (IRepository<SecondDbContext> repo = unitOfWork.CreateRepository<SecondDbContext>())
                    {
                        var validEntity = await repo.FirstOrDefaultAsync<SecondDbEntity>(f => f.CustomerName == "ValidCustomer2");
                        validEntity.ShouldBeNull();  // Ensure the valid entity was rolled back.
                    }
                }
            }
        }
    }

    /// <summary>
    /// Tests nested transactions using save points within a single DbContext.
    /// Ensures that changes can be partially committed or rolled back at different levels of nesting.
    /// </summary>
    /// <summary>
    /// Tests nested transactions using save points within a single DbContext.
    /// Ensures that changes can be partially committed up to a save point and then rolled back.
    /// </summary>
    [Fact]
    public async Task Case_UnitOfWork_NestedTransactionsWithSavePoints()
    {
        // Create an IHostBuilder and configure services to use TestApplicationDbContext with UnitOfWork.
        IHostBuilder host = Host.CreateDefaultBuilder().ConfigureServices((services) =>
        {
            // Configure TestApplicationDbContext with SQL Server settings.
            services.AddDbContextFactoryWithUnitOfWork<TestApplicationDbContext>(options =>
            {
                var cnnBuilder = new SqlConnectionStringBuilder(_sqlContainer.GetConnectionString());
                cnnBuilder.InitialCatalog = "Case_UnitOfWork_NestedTransactionsWithSavePoints";  // Set the initial catalog (database name).
                cnnBuilder.TrustServerCertificate = true;  // Trust the server certificate.
                cnnBuilder.MultipleActiveResultSets = true;  // Allow multiple active result sets.
                cnnBuilder.ConnectRetryCount = 5;  // Set the number of retry attempts for connection.
                cnnBuilder.ConnectTimeout = TimeSpan.FromMinutes(5).Seconds;  // Set connection timeout.
                options.UseSqlServer(cnnBuilder.ToString(), (opt) => opt.EnableRetryOnFailure());  // Use SQL Server with retry on failure.
                options.EnableSensitiveDataLogging();  // Enable logging of sensitive data (for debugging purposes).
                options.EnableDetailedErrors();  // Enable detailed error messages (for debugging purposes).
            });
        });

        // Build the IHost and get the required services for testing.
        using (IHost build = host.Build())
        {
            // Ensure TestApplicationDbContext database is created and clean up any existing data.
            var dbContextFactory = build.Services.GetRequiredService<IDbContextFactory<TestApplicationDbContext>>();
            using (var context = dbContextFactory.CreateDbContext())
            {
                context.Database.EnsureCreated();  // Ensure the database for TestApplicationDbContext is created.
                await context.CLEAN_TABLES_DO_NOT_USE_PRODUCTION();  // Clean up table records in TestApplicationDbContext.
            }

            // Begin a new request scope for dependency injection.
            using (IServiceScope requestScope = build.Services.CreateScope())
            using (var unitOfWork = requestScope.ServiceProvider.GetRequiredService<IUnitOfWork>())
            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                // Step 1: Insert a valid entity and commit the transaction.
                using (IRepository<TestApplicationDbContext> repo = unitOfWork.CreateRepository<TestApplicationDbContext>())
                {
                    var entity1 = new TestUserEntity { Name = "User1", Surname = "Surname1" };
                    await repo.InsertAsync(entity1, cancellationTokenSource.Token);
                    var commitSuccess1 = await unitOfWork.SaveChangesAsync(cancellationTokenSource.Token);  // Commit the transaction.
                    commitSuccess1.ShouldBeTrue();  // Ensure the commit was successful.
                }

                // Step 2: Insert another valid entity and save changes to create a save point.
                using (IRepository<TestApplicationDbContext> repo = unitOfWork.CreateRepository<TestApplicationDbContext>())
                {
                    var entity2 = new TestUserEntity { Name = "User2", Surname = "Surname2" };
                    await repo.InsertAsync(entity2, cancellationTokenSource.Token);

                    var savePointSuccess = await unitOfWork.SaveChangesAsync(cancellationTokenSource.Token);  // Commit changes to create a save point.
                    savePointSuccess.ShouldBeTrue();  // Ensure the save point was committed successfully.
                }

                // Step 3: Insert an invalid entity and simulate nested transaction rollback.
                try
                {
                    using (IRepository<TestApplicationDbContext> repo = unitOfWork.CreateRepository<TestApplicationDbContext>())
                    {
                        var entity3 = new TestUserEntity { Name = null, Surname = "Surname3" };  // Invalid entity.
                        await repo.InsertAsync(entity3, cancellationTokenSource.Token);

                        await unitOfWork.SaveChangesAsync(cancellationTokenSource.Token);  // Attempt to save changes.
                    }
                }
                catch (DbUpdateException)
                {
                    // Rollback to the save point due to invalid entity.
                    await unitOfWork.SaveChangesAsync(cancellationTokenSource.Token);  // This rollback should be automatic in most implementations, but we ensure it's explicitly checked.
                }

                // Step 4: Verify the second entity is still committed after rollback to save point.
                using (IRepository<TestApplicationDbContext> repo = unitOfWork.CreateRepository<TestApplicationDbContext>())
                {
                    var entity2 = await repo.FirstOrDefaultAsync<TestUserEntity>(f => f.Name == "User2");
                    entity2.ShouldNotBeNull();  // Ensure the second entity is still committed.
                }

                // Step 5: Verify the first entity is committed.
                using (IRepository<TestApplicationDbContext> repo = unitOfWork.CreateRepository<TestApplicationDbContext>())
                {
                    var entity1 = await repo.FirstOrDefaultAsync<TestUserEntity>(f => f.Name == "User1");
                    entity1.ShouldNotBeNull();  // Ensure the first entity is committed.
                }

                // Step 6: Verify the third entity was rolled back and not committed.
                using (IRepository<TestApplicationDbContext> repo = unitOfWork.CreateRepository<TestApplicationDbContext>())
                {
                    var entity3 = await repo.FirstOrDefaultAsync<TestUserEntity>(f => f.Surname == "Surname3");
                    entity3.ShouldBeNull();  // Ensure the third entity was rolled back.
                }
            }
        }
    }
}