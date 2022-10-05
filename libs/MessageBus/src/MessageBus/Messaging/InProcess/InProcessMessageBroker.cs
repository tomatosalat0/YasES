using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MessageBus.Messaging.InProcess.Channels;
using MessageBus.Messaging.InProcess.Execution;

namespace MessageBus.Messaging.InProcess
{
    public class InProcessMessageBroker : IMessageBroker, ICollectable, IExecutable
    {
        private const int BUFFERED_CHANNEL_REMOVE_SIZE = 20;

        private readonly LockedDictionary<TopicName, IWorkableChannel> _channels = new LockedDictionary<TopicName, IWorkableChannel>();
        private readonly BlockingCollection<object> _awakeSignals = new BlockingCollection<object>();
        private readonly BlockingCollection<Action> _pendingActions = new BlockingCollection<Action>();
        private readonly IScheduler _actionCollector;
        private readonly IScheduler _actionRunner;
        private readonly IEventExecuter _executer;
        private readonly ChannelCleanupTime _channelCleanupTimer;
        private bool _disposedValue;

        public InProcessMessageBroker(MessageBrokerOptions options)
        {
            if (options is null) throw new ArgumentNullException(nameof(options));

            _executer = options.EventExecuter;
            _channelCleanupTimer = new ChannelCleanupTime(interval: TimeSpan.FromSeconds(10));

            _actionCollector = options.CollectScheduler.Create(new ActionCollector(this));
            _actionRunner = options.ExecuteScheduler.Create(new ActionExecution(this));
        }

        public ISubscribable Commands(TopicName topic)
        {
            ThrowDisposed();
            return _channels.Write((channels) =>
            {
                if (!channels.TryGetValue(topic, out IWorkableChannel? result))
                {
                    result = new CommandsChannel(HandleAwake);
                    channels.Add(topic, result);
                }
                if (result is not CommandsChannel delegation)
                    throw new InvalidOperationException($"The provided topic '{topic}' is not a delegation channel.");
                return result;
            });
        }

        public ISubscribable Events(TopicName topic, EventsOptions options)
        {
            ThrowDisposed();
            return _channels.Write((channels) =>
            {
                if (!channels.TryGetValue(topic, out IWorkableChannel? result))
                {
                    result = new EventsChannel(HandleAwake);
                    if (options != EventsOptions.None)
                        ApplyOptions(topic, (EventsChannel)result, options);
                    channels.Add(topic, result);
                }
                if (result is not EventsChannel broadcast)
                    throw new InvalidOperationException($"The provided topic '{topic}' is not a events channel.");
                return result;
            });
        }

        private void ApplyOptions(TopicName topic, EventsChannel channel, EventsOptions options)
        {
            if ((options & EventsOptions.Temporary) == EventsOptions.Temporary)
                SetupTemporaryEventsChannel(topic, channel);
        }

        private void SetupTemporaryEventsChannel(TopicName topic, EventsChannel channel)
        {
            channel.OnLastSubscriptionRemoved = () =>
            {
                channel.OnLastSubscriptionRemoved = null;
                _channels.Write(channels =>
                {
                    if (channels.TryGetValue(topic, out var currentChannel) && currentChannel == channel)
                        channels.Remove(topic);
                });
            };
        }

        public Task Publish<T>(T message, IReadOnlyList<TopicName> topics)
        {
            if (message is null) throw new ArgumentNullException(nameof(message));
            if (topics is null) throw new ArgumentNullException(nameof(topics));
            if (topics.Count == 0)
                throw new ArgumentOutOfRangeException(nameof(topics), "A message must get published to at least one topic.");

            ThrowDisposed();

            IReadOnlyList<IChannel> targetChannels = _channels.Read(
                (channels) => topics
                    .Select(t => channels.GetValueOrDefault(t))
                    .WhereNotNull()
                    .ToArray()
            );

            return Task.WhenAll(targetChannels.Select(p => p.Publish(message)));
        }

        private void HandleAwake()
        {
            _awakeSignals.Add(new object());
        }

        private void ExecuteCleanup()
        {
            CleanupChannels();
        }

        private void CleanupChannels()
        {
            IWorkableChannel[] snapshot = _channels.Read((channels) => channels.Values.ToArray());
            foreach (var channel in snapshot)
                channel.Cleanup();
        }

        bool ICollectable.IsCompleted => _awakeSignals.IsCompleted;

        bool ICollectable.HasCollectables => _awakeSignals.Count > 0 || _channelCleanupTimer.ShouldCleanup();

        bool ICollectable.WaitFor(TimeSpan timeout, CancellationToken cancellationToken)
        {
            if (_channelCleanupTimer.ShouldCleanup())
                return true;
            return cancellationToken.CanBeCanceled
                ? _awakeSignals.TryTake(out _, (int)timeout.TotalMilliseconds, cancellationToken)
                : _awakeSignals.TryTake(out _, timeout);
        }

        void ICollectable.Collect()
        {
            IWorkableChannel[] snapshot = _channels.Read((channels) => channels.Values.ToArray());
            foreach (var work in snapshot.SelectMany(p => p.CollectWork()))
                _pendingActions.Add(work);

            if (_channelCleanupTimer.ShouldCleanup())
            {
                _channelCleanupTimer.Reset();
                _pendingActions.Add(ExecuteCleanup);
            }
        }

        bool IExecutable.IsCompleted => _pendingActions.IsCompleted;

        bool IExecutable.HasExecutables => _pendingActions.Count > 0;

        bool IExecutable.TryTake([NotNullWhen(true)] out Action? action, TimeSpan timeout, CancellationToken cancellationToken)
        {
            bool result = cancellationToken.CanBeCanceled
                ? _pendingActions.TryTake(out action, (int)timeout.TotalMilliseconds, cancellationToken)
                : _pendingActions.TryTake(out action, timeout);

            if (result)
                action = WrapForExecution(action!);

            return result;
        }

        private Action WrapForExecution(Action action)
        {
            return _executer.Wrap(action);
        }

        private void ThrowDisposed()
        {
            if (_disposedValue) throw new ObjectDisposedException(nameof(InProcessMessageBroker));
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _actionCollector.Dispose();
                    _actionRunner.Dispose();
                    _awakeSignals.Dispose();
                    _pendingActions.Dispose();
                    _channels.Dispose();
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
