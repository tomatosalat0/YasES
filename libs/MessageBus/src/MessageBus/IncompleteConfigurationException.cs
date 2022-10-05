using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace MessageBus
{
    [Serializable]
    [ExcludeFromCodeCoverage]
    public class IncompleteConfigurationException : Exception
    {
        public IncompleteConfigurationException()
        {
        }

        public IncompleteConfigurationException(string? message) : base(message)
        {
        }

        public IncompleteConfigurationException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected IncompleteConfigurationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
