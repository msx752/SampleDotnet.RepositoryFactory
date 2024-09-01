using Microsoft.EntityFrameworkCore;
using System;

namespace SampleDotnet.RepositoryFactory
{
    /// <summary>
    /// Represents the details of an exception thrown during a SaveChanges operation in a DbContext.
    /// </summary>
    public sealed class SaveChangesExceptionDetail
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SaveChangesExceptionDetail"/> class.
        /// </summary>
        /// <param name="context">The <see cref="DbContext"/> in which the exception was thrown.</param>
        /// <param name="exception">The <see cref="Exception"/> that was thrown.</param>
        internal SaveChangesExceptionDetail(DbContext context, Exception exception)
        {
            ExceptionThrownDbContext = context;
            Exception = exception;
        }

        /// <summary>
        /// Gets the <see cref="DbContext"/> instance where the exception was thrown.
        /// </summary>
        public DbContext ExceptionThrownDbContext { get; }

        /// <summary>
        /// Gets the <see cref="Exception"/> that was thrown during the SaveChanges operation.
        /// </summary>
        public Exception Exception { get; }
    }
}
