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
- If `IHasDateTimeOffset` interfece used on Entity object then value of the the CreatedAt and UpdatedAt properties will be updated automatically.
``` c#
        public class TestUserEntity : IHasDateTimeOffset
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
            public string Surname { get; set; }

            public DateTimeOffset? CreatedAt { get; set; }
            public DateTimeOffset? UpdatedAt { get; set; }
        }
```

