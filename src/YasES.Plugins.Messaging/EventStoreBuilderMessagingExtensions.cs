using System;
using MessageBus.Messaging;
using MessageBus.Messaging.InProcess;

namespace YasES.Core
{
    public static class EventStoreBuilderMessagingExtensions
    {
        /// <summary>
        /// Adds the <see cref="IMessageBroker"/> to the builder. This method creates the message broker with
        /// the default options (see <see cref="MessageBrokerOptions.Default"/>).
        /// </summary>
        public static NotificationEventStoreBuilder UseMessageBroker(this EventStoreBuilder builder)
        {
            return UseMessageBroker(builder, MessageBrokerOptions.Default());
        }

        /// <summary>
        /// Adds the <see cref="IMessageBroker"/> to the builder. Use the <paramref name="options"/> callback to
        /// create the scheduling instance which suits best for you.
        /// The returned object allows to add automatic event creation.
        /// </summary>
        public static NotificationEventStoreBuilder UseMessageBroker(this EventStoreBuilder builder, MessageBrokerOptions options)
        {
            if (builder is NotificationEventStoreBuilder)
                throw new InvalidOperationException($"You must only call 'UseMessageBroker' once.");
            return new NotificationEventStoreBuilder(builder, options);
        }
    }
}
