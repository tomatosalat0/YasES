using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MessageBus.Messaging
{
    public interface IMessageBroker : IDisposable
    {
        /// <summary>
        /// Gets the channel with the provided <paramref name="topic"/> name used for broadcasting events.
        /// If the channel didn't exist before, it will automatically get created.
        /// Event channels will not try to resend message if they weren't acknowledged.
        /// If multiple events are pending for a single event listener, each event
        /// will get forwarded to the listener separately. The handler won't get called simulatinously for
        /// multiple events. The execution order of messages is not guaranteed to be the
        /// same as the order the messages got published. The <paramref name="options"/> define the
        /// desired behavior of the events channel. If a channel is not created within this call,
        /// the provided value for <paramref name="options"/> is ignored.
        /// </summary>
        /// <exception cref="InvalidOperationException">Is thrown if the provided <paramref name="topic"/>
        /// already exists but it isn't a events channel.</exception>
        ISubscribable Events(TopicName topic, EventsOptions options);

        /// <summary>
        /// Gets the command channel with the provided <paramref name="topic"/> name.
        /// If the channel didn't exist before, it will automatically get created.
        /// Command channels will resend a message if it hasn't been acknowledged or rejected.
        /// A message will only get send to one subscriber. Each subscriber will only
        /// receive one message at a time. The execution order of messages is not guaranteed to be the
        /// same as the order the messages got published.
        /// </summary>
        /// <exception cref="InvalidOperationException">Is thrown if the provided <paramref name="topic"/>
        /// already exists but it isn't a command channel.</exception>
        ISubscribable Commands(TopicName topic);

        /// <summary>
        /// Publishes the provided <paramref name="message"/> to all
        /// channels specified in <paramref name="topics"/>. If a channel
        /// does not exist, it will get skipped silently.
        /// </summary>
        Task Publish<T>(T message, IReadOnlyList<TopicName> topics);
    }

    [Flags]
    public enum EventsOptions
    {
        /// <summary>
        /// No special channel options provided.
        /// </summary>
        None = 0,

        /// <summary>
        /// The channel is only used temporary and can be removed
        /// as soon as its last subscription was disposed.
        /// </summary>
        Temporary = 1
    }

    public static class IMessageBrokerExtensions
    {
        /// <summary>
        /// Gets the channel with the provided <paramref name="topic"/> name used for broadcasting events.
        /// If the channel didn't exist before, it will automatically get created.
        /// Event channels will not try to resend message if they weren't acknowledged.
        /// If multiple events are pending for a single event listener, each event
        /// will get forwarded to the listener separately. The handler won't get called simulatinously for
        /// multiple events. The execution order of messages is not guaranteed to be the
        /// same as the order the messages got published.
        /// </summary>
        /// <exception cref="InvalidOperationException">Is thrown if the provided <paramref name="topic"/>
        /// already exists but it isn't a events channel.</exception>
        public static ISubscribable Events(this IMessageBroker broker, TopicName topic)
        {
            return broker.Events(topic, EventsOptions.None);
        }

        public static Task Publish<T>(this IMessageBroker broker, T message, TopicName topic)
        {
            return broker.Publish(message, new[] { topic });
        }

        public static Task Publish<T>(this IMessageBroker broker, T message, TopicName topic, params TopicName[] otherTopics)
{
            return broker.Publish(message, new[] { topic }.Concat(otherTopics).ToArray());
        }
    }
}
