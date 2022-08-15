using System;
using System.Collections.Generic;
using System.Linq;

namespace YasES.Plugins.Messaging
{
    public interface IMessageBroker : IDisposable
    {
        /// <summary>
        /// Creates or opens the channel with the provided <paramref name="topic"/>.
        /// If the channel has not been used it, it will get created automatically.
        /// </summary>
        IChannel Channel(TopicName topic);

        /// <summary>
        /// Publishes the provided <paramref name="message"/> to all
        /// channels specified in <paramref name="topics"/>. If a channel
        /// does not exist, it will get skipped silently.
        /// </summary>
        IMessageBroker Publish<T>(T message, IEnumerable<TopicName> topics);
    }

    public static class IMessageBrokerExtensions
    {
        public static IMessageBroker Publish<T>(this IMessageBroker broker, T message, TopicName topic)
        {
            return broker.Publish(message, new[] { topic });
        }

        public static IMessageBroker Publish<T>(this IMessageBroker broker, T message, TopicName topic, params TopicName[] otherTopics)
        {
            return broker.Publish(message, new[] { topic }.Concat(otherTopics));
        }
    }
}
