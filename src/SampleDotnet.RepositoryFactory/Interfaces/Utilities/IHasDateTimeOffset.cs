namespace SampleDotnet.RepositoryFactory.Interfaces.Utilities;

/// <summary>
/// Defines properties for tracking the creation and last updated timestamps of an entity.
/// </summary>
public interface IHasDateTimeOffset
{
    /// <summary>
    /// Gets or sets the timestamp indicating when the entity was created.
    /// </summary>
    DateTimeOffset? CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp indicating when the entity was last updated.
    /// </summary>
    DateTimeOffset? UpdatedAt { get; set; }
}