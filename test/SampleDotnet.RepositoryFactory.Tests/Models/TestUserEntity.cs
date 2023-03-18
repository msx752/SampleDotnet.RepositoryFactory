namespace SampleDotnet.RepositoryFactory.Tests.Models;

public class TestUserEntity : IHasDateTimeOffset
{
    /// <summary>
    /// SELF NOTE: Use GUID for the PrimaryKey and SecondaryKey to be able to fix ID Conflict Exceptions when commiting the changes
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    public string Name { get; set; }
    public string Surname { get; set; }

    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}