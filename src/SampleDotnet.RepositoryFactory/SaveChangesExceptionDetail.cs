namespace SampleDotnet.RepositoryFactory;

public class SaveChangesExceptionDetail
{
    internal SaveChangesExceptionDetail(DbContext context, Exception exception)
    {
        ExceptionThrownDbContext = context;
        Exception = exception;
    }

    public DbContext ExceptionThrownDbContext { get; }
    public Exception Exception { get; }
}