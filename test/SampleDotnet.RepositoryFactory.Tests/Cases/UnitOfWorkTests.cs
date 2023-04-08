namespace SampleDotnet.RepositoryFactory.Tests.Cases;

public class UnitOfWorkTests
{
    [Fact]
    public async Task Case_UnitOfWork_Rollback()
    {
        IHostBuilder host = Host.CreateDefaultBuilder().ConfigureServices((services) =>
        {
            services.AddDbContextFactoryWithUnitOfWork<TestApplicationDbContext>(options =>
            {
                //var cnnBuilder = new SqlConnectionStringBuilder();
                //cnnBuilder.DataSource = "localhost,1433";
                //cnnBuilder.InitialCatalog = "TestApplicationDb";
                //cnnBuilder.TrustServerCertificate = true;
                //cnnBuilder.UserID = "sa";
                //cnnBuilder.Password = "Admin123!";
                //cnnBuilder.MultipleActiveResultSets = true;
                //cnnBuilder.ConnectRetryCount = 5;
                //cnnBuilder.ConnectTimeout = TimeSpan.FromMinutes(5).Seconds;
                //options.UseSqlServer(cnnBuilder.ToString());

                options.UseInMemoryDatabase("Case_UnitOfWork_Rollback");
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            });
        });

        using (IHost build = host.Build())
        //request scope
        using (IServiceScope requestScope = build.Services.CreateScope())
        using (var unitOfWork = requestScope.ServiceProvider.GetRequiredService<IUnitOfWork>())
        using (var cancellationTokenSource = new CancellationTokenSource())
        {
            //var testApplicationDbContextFactory = build.Services.GetRequiredService<IDbContextFactory<TestApplicationDbContext>>();
            //using (var context = testApplicationDbContextFactory.CreateDbContext())
            //{
            //    context.Database.EnsureCreated();
            //}

            TestUserEntity createdEntityFromFirstInstance = null;

            //instance 1 : Insert valid Entity
            {
                using (IRepository<TestApplicationDbContext> repo = unitOfWork.CreateRepository<TestApplicationDbContext>())
                {
                    TestUserEntity entity = new();
                    entity.Name = "TestName";
                    entity.Surname = "TestSurname";

                    entity.CreatedAt.ShouldBeNull();
                    entity.UpdatedAt.ShouldBeNull();

                    await repo.InsertAsync(entity, cancellationTokenSource.Token);

                    entity.CreatedAt.ShouldNotBeNull();
                    entity.UpdatedAt.ShouldBeNull();

                    createdEntityFromFirstInstance = entity;
                }
            }

            //instance 2 : Try-Select non-committed Entity
            {
                using (IRepository<TestApplicationDbContext> repo = unitOfWork.CreateRepository<TestApplicationDbContext>())
                {
                    TestUserEntity? entity = await repo.FirstOrDefaultAsync<TestUserEntity>(f => f.Name == "TestName" && f.Surname == "TestSurname", cancellationTokenSource.Token);

                    entity.ShouldBeNull();
                }
            }

            //instance 3 : Try-Update Entity's REQUIRED field to NULL which is tracked by different DbContext instance
            {
                using (IRepository<TestApplicationDbContext> repo = unitOfWork.CreateRepository<TestApplicationDbContext>())
                {
                    TestUserEntity? entity = await repo.FirstOrDefaultAsync<TestUserEntity>(f => f.Name == "TestName", cancellationTokenSource.Token);
                    entity.ShouldBeNull();

                    createdEntityFromFirstInstance.Name = null;

                    repo.Update(createdEntityFromFirstInstance);

                    createdEntityFromFirstInstance.CreatedAt.ShouldNotBeNull();
                    createdEntityFromFirstInstance.UpdatedAt.ShouldNotBeNull();
                }
            }

            //rollback changes
            await Assert.ThrowsAsync<DbUpdateException>(async () =>
              await unitOfWork.SaveChangesAsync(cancellationTokenSource.Token));

            //thrown exception details
            unitOfWork.SaveChangesException.ShouldNotBeNull();
            unitOfWork.IsDbConcurrencyExceptionThrown.ShouldBeFalse();

            //instance 4 : *** Try-Select reverted back Entity ***
            {
                using (IRepository<TestApplicationDbContext> repo = unitOfWork.CreateRepository<TestApplicationDbContext>())
                {
                    TestUserEntity? entity = await repo.FirstOrDefaultAsync<TestUserEntity>(f => f.Name == "TestName", cancellationTokenSource.Token);
                    entity.ShouldBeNull();

                    entity = await repo.FirstOrDefaultAsync<TestUserEntity>(f => f.Name == null, cancellationTokenSource.Token);
                    entity.ShouldBeNull();

                    var entities = repo.AsQueryable<TestUserEntity>().ToList();
                    entities.Count.ShouldBeEquivalentTo(0);
                }
            }
        }
    }

    [Fact]
    public async Task Case_UnitOfWork_TwoDifferent_DbContext_Rollback()
    {
        IHostBuilder host = Host.CreateDefaultBuilder().ConfigureServices((services) =>
        {
            services.AddDbContextFactoryWithUnitOfWork<FirstDbContext>(options =>
            {
                //var cnnBuilder = new SqlConnectionStringBuilder();
                //cnnBuilder.DataSource = "localhost,1433";
                //cnnBuilder.InitialCatalog = "FirstDb";
                //cnnBuilder.TrustServerCertificate = true;
                //cnnBuilder.UserID = "sa";
                //cnnBuilder.Password = "Admin123!";
                //cnnBuilder.MultipleActiveResultSets = true;
                //cnnBuilder.ConnectRetryCount = 5;
                //cnnBuilder.ConnectTimeout = TimeSpan.FromMinutes(5).Seconds;
                //options.UseSqlServer(cnnBuilder.ToString());

                options.UseInMemoryDatabase("FirstDbContext_Case_UnitOfWork");
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            });
            services.AddDbContextFactoryWithUnitOfWork<SecondDbContext>(options =>
            {
                //var cnnBuilder = new SqlConnectionStringBuilder();
                //cnnBuilder.DataSource = "localhost,1433";
                //cnnBuilder.InitialCatalog = "SecondDb";
                //cnnBuilder.TrustServerCertificate = true;
                //cnnBuilder.UserID = "sa";
                //cnnBuilder.Password = "Admin123!";
                //cnnBuilder.MultipleActiveResultSets = true;
                //cnnBuilder.ConnectRetryCount = 5;
                //cnnBuilder.ConnectTimeout = TimeSpan.FromMinutes(5).Seconds;
                //options.UseSqlServer(cnnBuilder.ToString());

                options.UseInMemoryDatabase("SecondDbContext_Case_UnitOfWork");
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            });
        });
        using (IHost build = host.Build())
        {
            //var firstDbContextFactory = build.Services.GetRequiredService<IDbContextFactory<FirstDbContext>>();
            //using (var context = firstDbContextFactory.CreateDbContext())
            //{
            //    context.Database.EnsureCreated();
            //}

            //var secondDbContextFactory = build.Services.GetRequiredService<IDbContextFactory<SecondDbContext>>();
            //using (var context = secondDbContextFactory.CreateDbContext())
            //{
            //    context.Database.EnsureCreated();
            //}

            //request scope
            using (IServiceScope requestScope = build.Services.CreateScope())
            using (var unitOfWork = requestScope.ServiceProvider.GetRequiredService<IUnitOfWork>())
            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                //instance 1 : Insert valid Entity
                {
                    using (IRepository<FirstDbContext> repo = unitOfWork.CreateRepository<FirstDbContext>())
                    {
                        FirstDbEntity entity = new();
                        entity.ProductName = "Product1";
                        entity.Price = 10.5M;

                        entity.CreatedAt.ShouldBeNull();
                        entity.UpdatedAt.ShouldBeNull();

                        await repo.InsertAsync(entity, cancellationTokenSource.Token);

                        entity.CreatedAt.ShouldNotBeNull();
                        entity.UpdatedAt.ShouldBeNull();
                    }
                }

                //instance 2 : Insert in-valid Entity
                {
                    using (IRepository<SecondDbContext> repo = unitOfWork.CreateRepository<SecondDbContext>())
                    {
                        SecondDbEntity entity = new();
                        entity.CustomerName = null;

                        entity.CreatedAt.ShouldBeNull();
                        entity.UpdatedAt.ShouldBeNull();

                        await repo.InsertAsync(entity, cancellationTokenSource.Token);

                        entity.CreatedAt.ShouldNotBeNull();
                        entity.UpdatedAt.ShouldBeNull();
                    }
                }

                //rollback changes
                await Assert.ThrowsAsync<DbUpdateException>(async () =>
                  await unitOfWork.SaveChangesAsync(cancellationTokenSource.Token));

                //thrown exception details
                unitOfWork.SaveChangesException.ShouldNotBeNull();
                unitOfWork.IsDbConcurrencyExceptionThrown.ShouldBeFalse();

                //instance 4 : *** Select reverted back Entity ***
                {
                    using (IRepository<FirstDbContext> repo = unitOfWork.CreateRepository<FirstDbContext>())
                    {
                        var entity = await repo.FirstOrDefaultAsync<FirstDbEntity>(f => f.ProductName == "Product1");
                        entity.ShouldBeNull();

                        var entities = repo.AsQueryable<FirstDbEntity>().ToList();
                        entities.Count.ShouldBeEquivalentTo(0);
                    }
                }

                //instance 5 : *** Select reverted back Entity ***
                {
                    using (IRepository<SecondDbContext> repo = unitOfWork.CreateRepository<SecondDbContext>())
                    {
                        var entity = await repo.FirstOrDefaultAsync<SecondDbEntity>(f => f.CustomerName == null);
                        entity.ShouldBeNull();

                        var entities = repo.AsQueryable<SecondDbEntity>().ToList();
                        entities.Count.ShouldBeEquivalentTo(0);
                    }
                }
            }
        }
    }

    [Fact]
    public async Task Case_UnitOfWork_CommitAndRollback()
    {
        IHostBuilder host = Host.CreateDefaultBuilder().ConfigureServices((services) =>
        {
            services.AddDbContextFactoryWithUnitOfWork<FirstDbContext>(options =>
            {
                //var cnnBuilder = new SqlConnectionStringBuilder();
                //cnnBuilder.DataSource = "localhost,1433";
                //cnnBuilder.InitialCatalog = "FirstDb";
                //cnnBuilder.TrustServerCertificate = true;
                //cnnBuilder.UserID = "sa";
                //cnnBuilder.Password = "Admin123!";
                //cnnBuilder.MultipleActiveResultSets = true;
                //cnnBuilder.ConnectRetryCount = 5;
                //cnnBuilder.ConnectTimeout = TimeSpan.FromMinutes(5).Seconds;
                //options.UseSqlServer(cnnBuilder.ToString());

                options.UseInMemoryDatabase("FirstDbContext_Case_UnitOfWork_CommitAndRollback");
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            });
            services.AddDbContextFactoryWithUnitOfWork<SecondDbContext>(options =>
            {
                //var cnnBuilder = new SqlConnectionStringBuilder();
                //cnnBuilder.DataSource = "localhost,1433";
                //cnnBuilder.InitialCatalog = "SecondDb";
                //cnnBuilder.TrustServerCertificate = true;
                //cnnBuilder.UserID = "sa";
                //cnnBuilder.Password = "Admin123!";
                //cnnBuilder.MultipleActiveResultSets = true;
                //cnnBuilder.ConnectRetryCount = 5;
                //cnnBuilder.ConnectTimeout = TimeSpan.FromMinutes(5).Seconds;
                //options.UseSqlServer(cnnBuilder.ToString());

                options.UseInMemoryDatabase("SecondDbContext_Case_UnitOfWork_CommitAndRollback");
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            });
        });
        using (IHost build = host.Build())
        {
            //request scope
            using (IServiceScope requestScope = build.Services.CreateScope())
            using (var unitOfWork = requestScope.ServiceProvider.GetRequiredService<IUnitOfWork>())
            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                //var firstDbContextFactory = build.Services.GetRequiredService<IDbContextFactory<FirstDbContext>>();
                //using (var context = firstDbContextFactory.CreateDbContext())
                //{
                //    context.Database.EnsureCreated();
                //}

                //var secondDbContextFactory = build.Services.GetRequiredService<IDbContextFactory<SecondDbContext>>();
                //using (var context = secondDbContextFactory.CreateDbContext())
                //{
                //    context.Database.EnsureCreated();
                //}

                //instance 1 : Insert valid Entity
                {
                    using (IRepository<FirstDbContext> repo = unitOfWork.CreateRepository<FirstDbContext>())
                    {
                        FirstDbEntity entity = new();
                        entity.ProductName = "Product1";
                        entity.Price = 10.5M;

                        entity.CreatedAt.ShouldBeNull();
                        entity.UpdatedAt.ShouldBeNull();

                        await repo.InsertAsync(entity, cancellationTokenSource.Token);

                        entity.CreatedAt.ShouldNotBeNull();
                        entity.UpdatedAt.ShouldBeNull();
                    }
                }

                //instance 2 : Insert valid Entity
                {
                    using (IRepository<SecondDbContext> repo = unitOfWork.CreateRepository<SecondDbContext>())
                    {
                        SecondDbEntity entity = new();
                        entity.CustomerName = "Customer1";

                        entity.CreatedAt.ShouldBeNull();
                        entity.UpdatedAt.ShouldBeNull();

                        await repo.InsertAsync(entity, cancellationTokenSource.Token);

                        entity.CreatedAt.ShouldNotBeNull();
                        entity.UpdatedAt.ShouldBeNull();
                    }
                }

                //commit 2 changes
                var isSucceed = await unitOfWork.SaveChangesAsync();
                isSucceed.ShouldBeTrue();

                //instance 3 : Select -> Update committed Entity
                {
                    using (IRepository<FirstDbContext> repo = unitOfWork.CreateRepository<FirstDbContext>(/*isolationLevel: System.Transactions.IsolationLevel.ReadCommitted*/))
                    {
                        var entity = await repo.FirstOrDefaultAsync<FirstDbEntity>(f => f.ProductName == "Product1");
                        entity.ShouldNotBeNull();

                        entity.ProductName = "Product1Updated";

                        repo.Update(entity);

                        entity.CreatedAt.ShouldNotBeNull();
                        entity.UpdatedAt.ShouldNotBeNull();
                    }
                }

                //instance 4 : Try-Insert invalid Entity
                {
                    using (IRepository<SecondDbContext> repo = unitOfWork.CreateRepository<SecondDbContext>())
                    {
                        SecondDbEntity entity = new();
                        entity.CustomerName = null;

                        entity.CreatedAt.ShouldBeNull();
                        entity.UpdatedAt.ShouldBeNull();

                        await repo.InsertAsync(entity, cancellationTokenSource.Token);

                        entity.CreatedAt.ShouldNotBeNull();
                        entity.UpdatedAt.ShouldBeNull();
                    }
                }

                //instance 5 : Select -> Delete committed Entity
                {
                    using (IRepository<SecondDbContext> repo = unitOfWork.CreateRepository<SecondDbContext>(/*isolationLevel: System.Transactions.IsolationLevel.ReadCommitted*/))
                    {
                        var entity = await repo.FirstOrDefaultAsync<SecondDbEntity>(f => f.CustomerName == "Customer1");
                        entity.ShouldNotBeNull();

                        repo.Delete(entity);
                    }
                }

                //rollback changes
                await Assert.ThrowsAsync<DbUpdateException>(async () =>
                  await unitOfWork.SaveChangesAsync(cancellationTokenSource.Token));

                //thrown exception details
                unitOfWork.SaveChangesException.ShouldNotBeNull();
                unitOfWork.IsDbConcurrencyExceptionThrown.ShouldBeFalse();

                //instance 6 : *** Select reverted back Entity ***
                {
                    using (IRepository<FirstDbContext> repo = unitOfWork.CreateRepository<FirstDbContext>(/*isolationLevel: System.Transactions.IsolationLevel.ReadCommitted*/))
                    {
                        var entity = await repo.FirstOrDefaultAsync<FirstDbEntity>(f => f.ProductName == "Product1");
                        entity.ShouldNotBeNull();
                    }
                }

                //instance 7 : *** Select reverted back Entity ***
                {
                    using (IRepository<SecondDbContext> repo = unitOfWork.CreateRepository<SecondDbContext>(/*isolationLevel: System.Transactions.IsolationLevel.ReadCommitted*/))
                    {
                        var entity = await repo.FirstOrDefaultAsync<SecondDbEntity>(f => f.CustomerName == "Customer1");
                        entity.ShouldNotBeNull();
                    }
                }

                //instance 8 : Try-Select discarded Entity changes
                {
                    using (IRepository<FirstDbContext> repo = unitOfWork.CreateRepository<FirstDbContext>(/*isolationLevel: System.Transactions.IsolationLevel.ReadCommitted*/))
                    {
                        var entity = await repo.FirstOrDefaultAsync<FirstDbEntity>(f => f.ProductName == "Product1Updated");
                        entity.ShouldBeNull();
                    }
                }

                //instance 9 : Try-Select discarded Entity changes
                {
                    using (IRepository<SecondDbContext> repo = unitOfWork.CreateRepository<SecondDbContext>(/*isolationLevel: System.Transactions.IsolationLevel.ReadCommitted*/))
                    {
                        var entity = await repo.FirstOrDefaultAsync<SecondDbEntity>(f => f.CustomerName == null);
                        entity.ShouldBeNull();
                    }
                }
            }
        }
    }

    [Fact]
    public async Task Case_UnitOfWork_Do_Not_Skip_DetectChanges()
    {
        IHostBuilder host = Host.CreateDefaultBuilder().ConfigureServices((services) =>
        {
            services.AddDbContextFactoryWithUnitOfWork<TestApplicationDbContext>(options =>
            {
                //var cnnBuilder = new SqlConnectionStringBuilder();
                //cnnBuilder.DataSource = "localhost,1433";
                //cnnBuilder.InitialCatalog = "TestApplicationDb";
                //cnnBuilder.TrustServerCertificate = true;
                //cnnBuilder.UserID = "sa";
                //cnnBuilder.Password = "Admin123!";
                //cnnBuilder.MultipleActiveResultSets = true;
                //cnnBuilder.ConnectRetryCount = 5;
                //cnnBuilder.ConnectTimeout = TimeSpan.FromMinutes(5).Seconds;
                //options.UseSqlServer(cnnBuilder.ToString());

                options.UseInMemoryDatabase("Case_UnitOfWork_Do_Not_Skip_DetectChanges");
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            });
        });

        using (IHost build = host.Build())
        //request scope
        using (IServiceScope requestScope = build.Services.CreateScope())
        using (var unitOfWork = requestScope.ServiceProvider.GetRequiredService<IUnitOfWork>())
        using (var cancellationTokenSource = new CancellationTokenSource())
        {
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
            }

            using (var repository = unitOfWork.CreateRepository<TestApplicationDbContext>())
            {
                var user1 = await repository.FirstOrDefaultAsync<TestUserEntity>(f => f.Surname == "Surname");

                user1.ShouldNotBeNull();
                user1.CreatedAt.ShouldNotBeNull();

                await unitOfWork.SaveChangesAsync();

                user1.Name = "NameUpdated2";

                await unitOfWork.SaveChangesAsync();

                user1.CreatedAt.ShouldNotBeNull();

                var user2 = await repository.FirstOrDefaultAsync<TestUserEntity>(f => f.Name == "NameUpdated2");
                user2.ShouldNotBeNull();
            }
        }
    }

    #region DbContext 1

    internal class FirstDbContext : DbContext
    {
        public DbSet<FirstDbEntity> FirstDbEntity { get; set; }

        public FirstDbContext(DbContextOptions<FirstDbContext> options)
            : base(options)
        {
        }
    }

    [Table("FirstDbEntity")]
    internal class FirstDbEntity : IHasDateTimeOffset
    {
        /// <summary>
        /// SELF NOTE: Use GUID for the PrimaryKey and SecondaryKey to be able to fix ID Conflict Exceptions when commiting the changes
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Required(AllowEmptyStrings = false)]
        public string? ProductName { get; set; }

        [Required]
        public Nullable<decimal> Price { get; set; }

        public DateTimeOffset? CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
    }

    #endregion DbContext 1

    #region DbContext 2

    internal class SecondDbContext : DbContext
    {
        public DbSet<SecondDbEntity> SecondDbEntity { get; set; }

        public SecondDbContext(DbContextOptions<SecondDbContext> options)
            : base(options)
        {
        }
    }

    [Table("SecondDbEntity")]
    internal class SecondDbEntity : IHasDateTimeOffset
    {
        /// <summary>
        /// SELF NOTE: Use GUID for the PrimaryKey and SecondaryKey to be able to fix ID Conflict Exceptions when commiting the changes
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Required(AllowEmptyStrings = false)]
        public string? CustomerName { get; set; }

        public DateTimeOffset? CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
    }

    #endregion DbContext 2
}