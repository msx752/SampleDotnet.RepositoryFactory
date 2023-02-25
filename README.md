[![Nuget](https://img.shields.io/badge/package-SampleDotnet.RepositoryFactory-brightgreen.svg?maxAge=259200)](https://www.nuget.org/packages/SampleDotnet.RepositoryFactory)
[![CodeQL](https://github.com/msx752/SampleDotnet.RepositoryFactory/actions/workflows/codeql.yml/badge.svg?branch=main)](https://github.com/msx752/SampleDotnet.RepositoryFactory/actions/workflows/codeql.yml)
[![MIT](https://img.shields.io/badge/License-MIT-blue.svg?maxAge=259200)](https://github.com/msx752/SampleDotnet.RepositoryFactory/blob/master/LICENSE.md)

# EFCore DbContext RepositoryFactory Pattern managed by DbContextFactory
EntityFrameworkCore doesn't support multiple parallel operations, when we need parallel actions in different threads such as adding or deleting on the same DbContext, It throws an exception when calling SaveChanges [source](https://learn.microsoft.com/en-us/ef/core/dbcontext-configuration/#avoiding-dbcontext-threading-issues).

NOTE: **DbContext service scope set as Transient which managed by IServiceScopeFactory**

# How to Use
``` c#
using SampleDotnet.RepositoryFactory;
```
ServiceCollection Definition
``` c#
services.AddDbContextFactory<UserDbContext>(opt =>
    opt.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));
```
then we call transient scoped DbContext
``` c#
    public class UserController : ControllerBase
    {
        private readonly IDbContextFactory<UserDbContext> _contextFactory;

        public UserController(IDbContextFactory<UserDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        [HttpGet("{id}")]
        public ActionResult Get(Guid id)
        {
            using (var repository = _contextFactory.CreateRepository())
            {
                var personal = repository.FirstOrDefault<UserEntity>(f => f.Id == id);

                //some operations goes here....

                repository.Delete(personal);

                //some operations goes here....

                repository.SaveChanges();
            }
        }
    }
```

# Additional Feature
- Changes Tracker for each Repository using 'IRepositoryEntryNotifier'
    - helps to manage entities before SaveChanges such as updating Adding or Updating Time on Entity

# Usage Example of the IRepositoryEntryNotifier
- we have a User entity which derived from [IHasTimestamps](https://learn.microsoft.com/en-us/ef/core/logging-events-diagnostics/events#example-timestamp-state-changes), when we add a new User or delete a User this event will be triggered by Repository

``` c#
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
    }

    public interface IHasTimestamps
    {
        DateTime CreatedAt { get; set; }
        DateTime UpdatedAt { get; set; }
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
                        //entity deleted on the database
                        //entityWithTimestamps.DeletedAt = DateTime.UtcNow;
                        break;
                }
            }
        }
    }
}
```

- ServiceCollection Definition (Singleton scope)
``` c#
services.AddSingleton<IRepositoryEntryNotifier, MyRpositoryNotifier>();
```

- after that call `var repository = _contextFactory.CreateRepository();` and add or delete entity