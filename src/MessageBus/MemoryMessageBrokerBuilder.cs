using MessageBus.Messaging;
using MessageBus.Messaging.InProcess;

namespace MessageBus
{
    public static class MemoryMessageBrokerBuilder
    {
        /// <summary>
        /// Creates a <see cref="IMessageBroker"/> which can be used within a single process.
        /// </summary>
        public static IMessageBroker InProcessBroker()
        {
            return new InProcessMessageBroker(MessageBrokerOptions.Default());
        }
    }
}
