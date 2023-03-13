namespace SampleDotnet.RepositoryFactory.Interfaces;

public interface IHasDateTimeOffset
{
    DateTimeOffset? CreatedAt { get; set; }
    DateTimeOffset? UpdatedAt { get; set; }
}