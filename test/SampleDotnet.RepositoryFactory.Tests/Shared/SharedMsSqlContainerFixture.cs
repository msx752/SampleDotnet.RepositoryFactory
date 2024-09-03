namespace SampleDotnet.RepositoryFactory.Tests.Shared;

public class SharedContainerFixture : IAsyncLifetime
{
    // A container for running SQL Server in Docker for testing purposes.
    public MsSqlContainer SqlContainer { get; }

    // Constructor initializes the SQL container with specific configurations.
    public SharedContainerFixture()
    {
        SqlContainer = new MsSqlBuilder()
            .WithPassword("Admin123!")  // Set the password for the SQL Server.
            .WithCleanUp(true)        // automatically clean up the container.
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(1433))  // Wait strategy to ensure SQL Server is ready.
            .Build();  // Build the container.
    }

    // DisposeAsync stops and disposes of the SQL container asynchronously after each test.
    public async Task DisposeAsync()
    {
        await SqlContainer.StopAsync();  // Stop the SQL Server container.
        await SqlContainer.DisposeAsync();  // Dispose of the SQL Server container.
    }

    // InitializeAsync starts the SQL container asynchronously before each test.
    public async Task InitializeAsync()
    {
        await SqlContainer.StartAsync();  // Start the SQL Server container.
    }
}