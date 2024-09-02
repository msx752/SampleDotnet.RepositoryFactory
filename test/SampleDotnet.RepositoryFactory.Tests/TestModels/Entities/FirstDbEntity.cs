using SampleDotnet.RepositoryFactory.Interfaces.Utilities;

namespace SampleDotnet.RepositoryFactory.Tests.TestModels.Entities;

// Represents an entity in the first database context.
[Table("FirstDbEntity")]
internal class FirstDbEntity : IHasDateTimeOffset
{
    public DateTimeOffset? CreatedAt { get; set; }

    /// <summary>
    /// SELF NOTE: Use GUID for the PrimaryKey and SecondaryKey to be able to fix ID Conflict Exceptions when committing the changes
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }  // Primary key.

    [Required]
    public decimal? Price { get; set; }

    [Required(AllowEmptyStrings = false)]
    public string? ProductName { get; set; }  // Product name, required and cannot be empty.

    // Price, required.

    // DateTime when entity was created.
    public DateTimeOffset? UpdatedAt { get; set; }  // DateTime when entity was last updated.
}