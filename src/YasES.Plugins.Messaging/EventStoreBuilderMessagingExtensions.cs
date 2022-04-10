using System;
using YasES.Plugins.Messaging;

namespace YasES.Core
{
    public static class EventStoreBuilderMessagingExtensions
    {
        /// <summary>
        /// Adds the <see cref="IMessageBroker"/> to the builder. This method creates a <see cref="ThreadedBrokerScheduling"/>
        /// instance to handle the broker commands. The number of threads is calculated by the number of available CPU cores divided
        /// by 2, with a minimum number of 1 and a maximum number of 5.
        /// The returned object allows to add automatic event creation.
        /// </summary>
        public static NotificationEventStoreBuilder UseMessageBroker(this EventStoreBuilder builder)
        {
            int numberOfThreads = Math.Max(1, Math.Min(5, Environment.ProcessorCount / 2));
            return UseMessageBroker(builder, (c) => new ThreadedBrokerScheduling(numberOfThreads, c));
        }

        /// <summary>
        /// Adds the <see cref="IMessageBroker"/> to the builder. Use the <paramref name="initialization"/> callback to
        /// create the scheduling instance which suits best for you.
        /// The returned object allows to add automatic event creation.
        /// </summary>
        public static NotificationEventStoreBuilder UseMessageBroker(this EventStoreBuilder builder, Func<IBrokerCommands, IDisposable?> initialization)
        {
            if (builder is NotificationEventStoreBuilder)
                throw new InvalidOperationException($"You must only call 'UseMessageBroker' once.");
            return new NotificationEventStoreBuilder(builder, initialization);
        }
    }
}
