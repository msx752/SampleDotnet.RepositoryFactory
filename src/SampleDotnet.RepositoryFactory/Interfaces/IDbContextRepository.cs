namespace SampleDotnet.RepositoryFactory.Interfaces;

/// <summary>
/// Interface for repository operations specific to a DbContext, providing access to the ChangeTracker and Database.
/// </summary>
public interface IDbContextRepository
{
    /// <summary>
    /// Gets the ChangeTracker associated with the DbContext.
    /// </summary>
    ChangeTracker ChangeTracker { get; }

    /// <summary>
    /// Gets the DatabaseFacade associated with the DbContext.
    /// </summary>
    DatabaseFacade Database { get; }
}
