using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MessageBus.Messaging.InProcess.Messages;

namespace MessageBus.Messaging.InProcess.Channels
{
    internal class EventsChannel : IWorkableChannel
    {
        private readonly LockedList<ChannelSubscription> _allSubscriptions = new LockedList<ChannelSubscription>();
        private readonly LockedList<ChannelSubscription> _availableSubscriptions = new LockedList<ChannelSubscription>();
        private readonly Action _awake;
        private int _subscriptionCounter;

        public EventsChannel(Action awake)
        {
            _awake = awake ?? throw new ArgumentNullException(nameof(awake));
        }

        public Action? OnLastSubscriptionRemoved { get; set; }

        public void Cleanup()
        {
            _availableSubscriptions.Write(list =>
            {
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    ChannelSubscription s = list[i];
                    if (!s.IsActive)
                    {
                        list.RemoveAt(i);
                    }
                }
            });
            _allSubscriptions.RemoveWhere(p => !p.IsActive);
        }

        public IReadOnlyList<Action> CollectWork()
        {
            return _availableSubscriptions.Write<IReadOnlyList<Action>>(list =>
            {
                if (list.Count == 0)
                    return Array.Empty<Action>();

                List<Action> result = new List<Action>();
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    ChannelSubscription s = list[i];
                    if (!s.IsActive)
                    {
                        list.RemoveAt(i);
                    }
                    else
                    if (s.HasMessages)
                    {
                        list.RemoveAt(i);
                        result.Add(BuildWorkAction(s));
                    }
                }
                return result;
            });
        }

        private static Action BuildWorkAction(ChannelSubscription subscription)
        {
            return subscription.DoWorkAction;
        }

        private void HandleWorkComplete(ChannelSubscription subscription)
        {
            if (!subscription.IsActive)
                return;

            _availableSubscriptions.Add(subscription);
            if (subscription.HasMessages)
                _awake();
        }

        private static Message? NoAdHocFetch()
        {
            return null;
        }

        public IDisposable Subscribe<T>(Action<IMessage<T>> messageHandler) where T : notnull
        {
            if (messageHandler is null) throw new ArgumentNullException(nameof(messageHandler));

            ChannelSubscription subscription = new ChannelSubscription(WrapExceptionHandler(a => messageHandler((Message<T>)a)), HandleWorkComplete, NoAdHocFetch);
            _allSubscriptions.Add(subscription);
            _availableSubscriptions.Add(subscription);

            IDisposable result = subscription;
            if (OnLastSubscriptionRemoved is not null)
                result = TrackSubscription(result);
            return result;
        }

        public IDisposable Subscribe(Action<IMessage> messageHandler)
        {
            if (messageHandler is null) throw new ArgumentNullException(nameof(messageHandler));

            ChannelSubscription subscription = new ChannelSubscription(WrapExceptionHandler(a => messageHandler(a)), HandleWorkComplete, NoAdHocFetch);
            _allSubscriptions.Add(subscription);
            _availableSubscriptions.Add(subscription);

            IDisposable result = subscription;
            if (OnLastSubscriptionRemoved is not null)
                result = TrackSubscription(result);
            return result;
        }

        private IDisposable TrackSubscription(IDisposable subscription)
        {
            return new TrackedSubscription(this, subscription);
        }

        private static Action<Message> WrapExceptionHandler(Action<Message> handler)
        {
            return (m) =>
            {
                try
                {
                    handler(m);
                    HandleAfterHandle(m);
                }
                catch (Exception ex)
                {
                    HandleException(m, ex);
                }
            };
        }

        private static void HandleAfterHandle(Message message)
        {
        }

        private static void HandleException(Message message, Exception exception)
        {
        }

        public Task Publish<T>(T message)
        {
            if (message is null) throw new ArgumentNullException(nameof(message));

            int numberOfPublishes = _allSubscriptions.ForEach(channel => channel.IsActive, channel => channel.Enqueue(new Message(message)));
            if (numberOfPublishes > 0)
                _awake();

            return Task.CompletedTask;
        }

        private class TrackedSubscription : IDisposable
        {
            private readonly IDisposable _subscription;
            private readonly EventsChannel _parent;
            private bool _disposedValue;

            public TrackedSubscription(EventsChannel parent, IDisposable subscription)
            {
                _parent = parent;
                _subscription = subscription;
                Interlocked.Increment(ref _parent._subscriptionCounter);
            }

            private void HandleSubscriptionClosed()
            {
                if (Interlocked.Decrement(ref _parent._subscriptionCounter) == 0)
                    _parent.OnLastSubscriptionRemoved?.Invoke();
            }

            protected virtual void Dispose(bool disposing)
            {
                if (!_disposedValue)
                {
                    if (disposing)
                    {
                        _subscription.Dispose();
                        HandleSubscriptionClosed();
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
}
