namespace SampleDotnet.RepositoryFactory.Tests.TestModels.Entities;

// Represents an entity in the second database context.
[Table("SecondDbEntity")]
internal class SecondDbEntity : IHasDateTimeOffset
{
    public DateTimeOffset? CreatedAt { get; set; }

    [Required(AllowEmptyStrings = false)]
    public string? CustomerName { get; set; }

    /// <summary>
    /// SELF NOTE: Use GUID for the PrimaryKey and SecondaryKey to be able to fix ID Conflict Exceptions when committing the changes
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }  // Primary key.

    // Customer name, required and cannot be empty.

    // DateTime when entity was created.
    public DateTimeOffset? UpdatedAt { get; set; }  // DateTime when entity was last updated.
}