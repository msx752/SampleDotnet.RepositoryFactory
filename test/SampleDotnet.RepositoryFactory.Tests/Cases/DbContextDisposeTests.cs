﻿namespace SampleDotnet.RepositoryFactory.Tests.Cases
{
    public class DbContextDisposeTests
    {
        [Fact]
        public async Task Case_DbContext_Should_Not_Throw_ObjectDisposedException()
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

                    options.UseInMemoryDatabase("Case_DbContext_Should_Not_Throw_ObjectDisposedException");
                    options.EnableSensitiveDataLogging();
                    options.EnableDetailedErrors();
                });
            });

            using (IHost build = host.Build())
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
}