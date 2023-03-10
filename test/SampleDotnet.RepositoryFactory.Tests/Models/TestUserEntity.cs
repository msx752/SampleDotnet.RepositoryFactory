namespace SampleDotnet.RepositoryFactory.Tests.Models;

public class TestUserEntity : IHasDateTimeOffset
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Surname { get; set; }

    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}