namespace SampleDotnet.RepositoryFactory.Tests.Extensions;

public static class DbContextOptionsBuilderExtensions
{
    public static void UseTestSqlConnection(this DbContextOptionsBuilder options, SharedContainerFixture shared, string initialCatalog)
    {
        var cnnBuilder = new SqlConnectionStringBuilder(shared.SqlContainer.GetConnectionString());
        cnnBuilder.InitialCatalog = initialCatalog;
        cnnBuilder.TrustServerCertificate = true;
        cnnBuilder.MultipleActiveResultSets = true;
        cnnBuilder.ConnectRetryCount = 5;
        cnnBuilder.ConnectTimeout = TimeSpan.FromMinutes(5).Seconds;
        options.UseSqlServer(cnnBuilder.ToString(), opt => opt.EnableRetryOnFailure());
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
}
