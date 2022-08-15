using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace YasES.Plugins.Messaging
{
    internal class MessageBroker : IMessageBroker, IBrokerCommands
    {
        private readonly Dictionary<TopicName, Channel> _channels = new Dictionary<TopicName, Channel>();
        private readonly object _channelsLock = new object();
        private readonly BlockingCollection<object> _wakeUpNotifications = new BlockingCollection<object>();
        private readonly IDisposable? _schedulingHandle;
        private bool _disposedValue;

        public MessageBroker(Func<IBrokerCommands, IDisposable?> initialization)
        {
            if (initialization is null) throw new ArgumentNullException(nameof(initialization));
            _schedulingHandle = initialization(this);
        }

        public IChannel Channel(TopicName topic)
        {
            ThrowDisposed();
            lock (_channelsLock)
            {
                if (!_channels.TryGetValue(topic, out Channel? result))
                {
                    result = new Channel(ChannelGotPublishEvent);
                    _channels.Add(topic, result);
                }
                return result;
            }
        }

        public int ActiveChannels
        {
            get
            {
                lock (_channelsLock)
                    return _channels.Count;
            }
        }

        public IMessageBroker Publish<T>(T message, IEnumerable<TopicName> topics)
        {
            if (message is null) throw new ArgumentNullException(nameof(message));
            if (topics is null) throw new ArgumentNullException(nameof(topics));
            ThrowDisposed();

            lock (_channelsLock)
            {
                foreach (var topic in topics)
                {
                    if (!_channels.TryGetValue(topic, out Channel? channel))
                        continue;

                    channel.Publish(message);
                }
            }
            return this;
        }

        private void ChannelGotPublishEvent(Channel channel)
        {
            _wakeUpNotifications.Add(new object());
        }

        bool IBrokerCommands.WaitForMessages(TimeSpan timeout, CancellationToken cancellationToken)
        {
            return _wakeUpNotifications.TryTake(out var _, (int)timeout.TotalMilliseconds, cancellationToken);
        }

        void IBrokerCommands.RemoveEmptyChannels()
        {
            lock (_channelsLock)
            {
                List<TopicName> emptyChannels = new List<TopicName>();
                foreach (var pair in _channels)
                {
                    if (pair.Value.IsEmpty)
                        emptyChannels.Add(pair.Key);
                }

                foreach (var channelName in emptyChannels)
                    _channels.Remove(channelName);
            }
        }

        int IBrokerCommands.CallSubscribers()
        {
            int numberOfExecutedSubscriptions = 0;
            List<Channel> channels = new List<Channel>();
            lock (_channelsLock)
                channels.AddRange(_channels.Values);

            foreach (var channel in channels)
            {
                numberOfExecutedSubscriptions += channel.SendQueueMessages();
            }
            return numberOfExecutedSubscriptions;
        }

        private void ThrowDisposed()
        {
            if (_disposedValue) throw new ObjectDisposedException(nameof(MessageBroker));
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _schedulingHandle?.Dispose();
                    _wakeUpNotifications.Dispose();
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
