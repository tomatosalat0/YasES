using System;
using System.Collections.Concurrent;
using System.Reflection;
using MessageBus.Messaging;

namespace MessageBus
{
    /// <summary>
    /// A simple event bus which uses <see cref="IMessageBroker"/> as the event transport mechansim.
    /// </summary>
    public partial class MessageBrokerMessageBus : IMessageBus, IDisposable
    {
        private readonly ConcurrentDictionary<Type, TopicName> _topicNameCache = new ConcurrentDictionary<Type, TopicName>();
        private readonly IExceptionNotification _exceptionNotification;
        private readonly IMessageBroker _broker;
        private bool _disposedValue;

        public MessageBrokerMessageBus(IMessageBroker broker, IExceptionNotification exceptionNotification)
        {
            _broker = broker;
            _exceptionNotification = exceptionNotification ?? throw new ArgumentNullException(nameof(exceptionNotification));
        }

        private TopicName GetOutcomeTopicName<TType>(TType request)
            where TType : IHasMessageId
        {
            TopicName commandTopic = GetTopicNameFromType(typeof(TType));
            return new TopicName($"{commandTopic}.Outcome.{request.MessageId}");
        }

        private void ThrowDisposed()
        {
            if (_disposedValue)
                throw new ObjectDisposedException(nameof(MessageBrokerMessageBus));
        }

        private TopicName GetTopicNameFromType(Type type)
        {
            return _topicNameCache.GetOrAdd(type, ReadTopicNameFromAttribute);
        }

        private static TopicName ReadTopicNameFromAttribute(Type type)
        {
            TopicAttribute? attribute = type.GetCustomAttribute<TopicAttribute>();
            if (attribute is null)
                throw new IncompleteConfigurationException($"The event '{type.FullName}' doesn't have the required Topic attribute.");

            return attribute.Topic;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _broker.Dispose();
                }
                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
