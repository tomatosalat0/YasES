using System;
using System.Collections.Generic;

namespace YasES.Plugins.Messaging
{
    internal class Channel : IChannel
    {
        private readonly List<Queue> _queues = new List<Queue>();
        private readonly object _queuesLock = new object();
        private readonly Action<Channel> _publishCallback;

        public Channel(Action<Channel> publishCallback)
        {
            _publishCallback = publishCallback ?? throw new ArgumentNullException(nameof(publishCallback));
        }

        internal bool IsEmpty
        {
            get
            {
                lock (_queuesLock)
                    return _queues.Count == 0;
            }
        }

        public void Publish<T>(T message)
        {
            if (message is null) throw new ArgumentNullException(nameof(message));

            IReadOnlyList<Queue> queuesToProcess = CreateSnapshotOfAllQueues();
            if (queuesToProcess.Count == 0)
                return;

            foreach (var queue in queuesToProcess)
                queue.Enqueue(message);

            NotifiyPublishCallback();
        }

        private void NotifiyPublishCallback()
        {
            try
            {
                _publishCallback(this);
            }
            catch (Exception)
            {
            }
        }

        public IDisposable Subscribe<T>(Action<IMessage<T>> messageHandler)
        {
            Queue subscriberQueue = CreateQueue(messageHandler);
            return AddQueue(subscriberQueue);
        }

        public IDisposable Subscribe(Action<IMessage> messageHandler)
        {
            Queue subscriberQueue = CreateQueue(messageHandler);
            return AddQueue(subscriberQueue);
        }

        internal IReadOnlyList<Queue> CreateSnapshotOfAllQueues()
        {
            List<Queue> result = new List<Queue>();
            lock (_queuesLock)
                result.AddRange(_queues);
            return result;
        }

        private Queue CreateQueue<T>(Action<IMessage<T>> messageHandler)
        {
            if (messageHandler is null) throw new ArgumentNullException(nameof(messageHandler));
            Queue subscriberQueue = new Queue((message) => messageHandler((Message<T>)message));
            return subscriberQueue;
        }

        private Queue CreateQueue(Action<IMessage> messageHandler)
        {
            if (messageHandler is null) throw new ArgumentNullException(nameof(messageHandler));
            Queue subscriberQueue = new Queue((message) => messageHandler(message));
            return subscriberQueue;
        }

        private IDisposable AddQueue(Queue queue)
        {
            lock (_queuesLock)
                _queues.Add(queue);
            SubscriptionQueue result = new SubscriptionQueue(this, queue);
            return result;
        }

        private void RemoveQueue(Queue queue)
        {
            lock (_queuesLock)
            {
                _queues.Remove(queue);
            }
        }

        internal int SendQueueMessages()
        {
            int result = 0;
            foreach (var queue in CreateSnapshotOfAllQueues())
            {
                if (queue.Send())
                    result++;
            }
            return result;
        }

        private sealed class SubscriptionQueue : IDisposable
        {
            private readonly Channel _channel;
            private readonly Queue _queue;
            private bool _disposedValue;

            public SubscriptionQueue(Channel channel, Queue queue)
            {
                _channel = channel;
                _queue = queue;
            }

            public void Dispose()
            {
                if (!_disposedValue)
                {
                    _disposedValue = true;
                    _channel.RemoveQueue(_queue);
                }
                GC.SuppressFinalize(this);
            }
        }
    }
}
