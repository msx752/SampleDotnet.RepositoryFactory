using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using SampleDotnet.RepositoryFactory.Interfaces;

namespace SampleDotnet.RepositoryFactory.Tests
{
    public class UserEntity : IHasTimestamps
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string SurName { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime DeletedAt { get; set; }
    }

    public interface IHasTimestamps
    {
        DateTime CreatedAt { get; set; }
        DateTime UpdatedAt { get; set; }
        DateTime DeletedAt { get; set; }
    }

    public class MyRpositoryNotifier : IRepositoryEntryNotifier
    {
        public void RepositoryEntryEvent(object sender, EntityEntryEventArgs e, DbContext dbContext, IServiceProvider serviceProvider)
        {
            if (e.Entry.State == EntityState.Unchanged || e.Entry.State == EntityState.Detached)
                return;

            if (e.Entry.Entity is IHasTimestamps entityWithTimestamps)
            {
                switch (e.Entry.State)
                {
                    case EntityState.Added:
                        entityWithTimestamps.CreatedAt = DateTime.UtcNow;
                        break;

                    case EntityState.Modified:
                        entityWithTimestamps.UpdatedAt = DateTime.UtcNow;
                        break;

                    case EntityState.Deleted:
                        entityWithTimestamps.DeletedAt = DateTime.UtcNow;
                        break;
                }
            }
        }
    }
}