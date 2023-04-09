using System.Runtime.Serialization;

namespace SampleDotnet.RepositoryFactory
{
    public class RollbackException : Exception
    {
        public RollbackException()
        {
        }

        public RollbackException(string? message) : base(message)
        {
        }

        public RollbackException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected RollbackException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}