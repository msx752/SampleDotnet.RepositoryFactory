using Microsoft.Data.SqlClient;

namespace SampleDotnet.RepositoryFactory.Tests;

public class TestApplicationDbContext : DbContext
{
    public DbSet<TestUserEntity> UserEntity { get; set; }

    public TestApplicationDbContext(DbContextOptions<TestApplicationDbContext> options)
        : base(options)
    {
    }
}