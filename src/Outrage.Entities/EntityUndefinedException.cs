using System.Runtime.Serialization;

namespace Outrage.Entities
{
    [Serializable]
    public class EntityUndefinedException : Exception
    {
        public EntityUndefinedException()
        {
        }

        public EntityUndefinedException(string? message) : base(message)
        {
        }

        public EntityUndefinedException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected EntityUndefinedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}