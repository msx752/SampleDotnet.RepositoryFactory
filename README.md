[![Nuget](https://img.shields.io/badge/package-SampleDotnet.RepositoryFactory-brightgreen.svg?maxAge=259200)](https://www.nuget.org/packages/SampleDotnet.RepositoryFactory)
[![CodeQL](https://github.com/msx752/SampleDotnet.RepositoryFactory/actions/workflows/codeql.yml/badge.svg?branch=main)](https://github.com/msx752/SampleDotnet.RepositoryFactory/actions/workflows/codeql.yml)
[![MIT](https://img.shields.io/badge/License-MIT-blue.svg?maxAge=259200)](https://github.com/msx752/SampleDotnet.RepositoryFactory/blob/master/LICENSE.md)

# **SampleDotnet.RepositoryFactory Documentation**

## **Overview**

The **SampleDotnet.RepositoryFactory** NuGet package provides a robust and flexible solution for managing data access using Entity Framework Core (EF Core) in .NET applications. It leverages the **Repository Pattern** and **Unit of Work Pattern** to ensure efficient database operations, transaction management, and better separation of concerns. This package simplifies working with multiple `DbContext` instances and supports parallel operations using transient scoped `DbContexts` managed by a `DbContextFactory`.

> **Note**: Most features in this package are currently in a preview version. While the package is stable for general use, some features may still be undergoing testing and improvements. Please provide feedback and report any issues to help us enhance the package further.

## **Key Features**

- **Repository Pattern**: Provides generic repositories to manage entities with CRUD operations.
- **Unit of Work Pattern**: Encapsulates transaction management and ensures all operations are completed or rolled back together.
- **DbContextFactory**: Supports transient scoped `DbContexts` to prevent concurrency issues.
- **Automatic Property Management**: Automatically updates `CreatedAt` and `UpdatedAt` properties for entities implementing `IHasDateTimeOffset`.
- **Flexible Service Lifetimes**: Supports configuring services with various lifetimes (Scoped, Transient, Singleton) to suit different application needs.

## **Installation**

To install the **SampleDotnet.RepositoryFactory** NuGet package, run the following command in the **Package Manager Console**:

```shell
Install-Package SampleDotnet.RepositoryFactory -Pre
```

Or use the **.NET CLI**:

```shell
dotnet add package SampleDotnet.RepositoryFactory --prerelease
```

### **Prerequisites**

Ensure that your project targets one of the following frameworks: **.NET 6.0**, **.NET 7.0**, or **.NET 8.0**. This package does not support .NET 5.

## **Usage**

### **1. Service Registration**

To use the `RepositoryFactory` and `UnitOfWork` in your application, register the necessary services in the `Startup` class or wherever you configure services in your application.

#### **Example: Configuring Services in ASP.NET Core**

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // Add DbContextFactory for UserDbContext with the desired lifetime
    services.AddDbContextFactory<UserDbContext>(options =>
    {
        options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"));
    });

    // Add repository factory with a specified service lifetime "Scoped or Transient (especially for the Consumers)"
    services.AddRepositoryFactory(ServiceLifetime.Scoped);

    // Additional service registrations...
}
```

This setup uses the traditional `AddDbContextFactory` method to configure `DbContextFactory` for `UserDbContext` and specifies the desired service lifetime for the factory.

### **2. Using the Repository and Unit of Work in Controllers**

After configuring the services, use the `IUnitOfWork` and repository pattern in your controllers to manage database operations.

#### **Example: UserController**

```csharp
public class UserController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    public UserController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpDelete("{id}")]
    public ActionResult Delete(Guid id)
    {
        using (var repository = _unitOfWork.CreateRepository<UserDbContext>())
        {
            var user = repository.FirstOrDefault<UserEntity>(u => u.Id == id);

            // Perform operations on the entity...

            repository.Delete(user);

            // Additional operations...
        }

        _unitOfWork.SaveChanges();
        return Ok();
    }
}
```

### **3. Automatic Timestamps for Entities**

If your entities implement the `IHasDateTimeOffset` interface, their `CreatedAt` and `UpdatedAt` properties will be automatically managed when using repository methods.

#### **Example: TestUserEntity**

```csharp
public class TestUserEntity : IHasDateTimeOffset
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    public string Name { get; set; }
    public string Surname { get; set; }

    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
```

### **4. Parallel Operations and DbContextFactory**

The package supports parallel operations by using a `DbContextFactory` to create new `DbContext` instances. This approach prevents concurrency issues that occur when the same `DbContext` instance is used across multiple threads.

## **Special Note on Transaction Management**

> **IMPORTANT**: Proper transaction management is crucial when working with multiple `DbContext` instances.

### **Key Points to Consider:**

- **Using Multiple DbContexts**: If you are using multiple `DbContext` instances, each managing its own transaction, be aware that if a transaction commits successfully on one `DbContext` but fails on another, you may need to manually roll back already committed transactions to maintain data consistency. This scenario requires careful handling to ensure that all related changes are either fully committed or completely rolled back across all DbContexts.

- **Using a Single DbContext**: When using a single `DbContext` instance for all operations within a unit of work, transaction management is simplified. All operations are either committed together or rolled back together, eliminating the need to manually roll back already committed transactions from different `DbContext` instances.

### **Best Practice**: To avoid the complexity of handling multiple transactions, use a single `DbContext` instance within the scope of a unit of work whenever possible. This approach ensures straightforward transaction management and helps maintain data integrity.

## **Descriptions of Key Components**

### **IUnitOfWork Interface**

The `IUnitOfWork` interface defines methods for managing database transactions and repositories:
- **`CreateRepository<TDbContext>()`**: Creates a new repository for the specified `DbContext`.
- **`SaveChanges()`**: Commits all pending changes to the database.
- **`SaveChangesAsync()`**: Asynchronously commits all pending changes to the database.
- **`IsDbConcurrencyExceptionThrown`**: Indicates if a concurrency exception occurred.
- **`SaveChangesException`**: Provides details about any exception thrown during the save operation.

### **Repository<TDbContext> Class**

A generic repository class that provides CRUD operations and additional methods for querying and manipulating entities:
- **`AsQueryable<T>()`**: Returns an `IQueryable` for the specified entity type.
- **`Delete<T>()`**: Deletes an entity.
- **`Find<T>()` and `FindAsync<T>()`**: Finds entities by primary key values.
- **`Insert<T>()` and `InsertAsync<T>()`**: Inserts new entities.
- **`Update<T>()`**: Updates entities.

### **DbContextFactory and Service Lifetimes**

The `DbContextFactory` can now be configured with different lifetimes (Scoped, Transient, Singleton) based on your application's needs. Using a transient lifetime is particularly useful for avoiding concurrency issues in multi-threaded environments.

### **Transaction Management**

The `UnitOfWork` and `TransactionManager` handle transaction management, ensuring that all operations within a unit of work are either committed or rolled back together.

## **Best Practices**

- **Use Scoped or Transient Services**: Use scoped or transient services for the `UnitOfWork` and related services depending on your application's concurrency requirements.
- **Avoid Long-Running Operations**: Keep the `DbContext` lifespan short to avoid holding resources longer than necessary.
- **Handle Exceptions Gracefully**: Use `IsDbConcurrencyExceptionThrown` and `SaveChangesException` to handle concurrency and other exceptions effectively.

## **Conclusion**

The **SampleDotnet.RepositoryFactory** NuGet package provides a streamlined way to manage data access in .NET applications, leveraging best practices for transaction management and parallel operations. With support for various service lifetimes, you can now tailor the package to suit your application's specific needs. By following this guide, you can easily integrate this package into your projects and take advantage of its robust features.

For more details or support, please refer to the package documentation or reach out to the community for assistance.
