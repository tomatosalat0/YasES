using System;
using System.Collections.Concurrent;

namespace YasES.Plugins.Messaging
{
    internal class Queue
    {
        private readonly ConcurrentQueue<Message> _pendingMessages = new ConcurrentQueue<Message>();
        private readonly Action<Message> _subscriber;

        public Queue(Action<Message> subscriber)
        {
            _subscriber = subscriber ?? throw new ArgumentNullException(nameof(subscriber));
        }

        public bool IsEmpty => _pendingMessages.IsEmpty;

        public void Enqueue(object data)
        {
            if (data is null) throw new ArgumentNullException(nameof(data));
            _pendingMessages.Enqueue(new Message(data));
        }

        /// <summary>
        /// Send the next message to the subscriber. Will return 
        /// true if there was any message, otherwise false.
        /// </summary>
        public bool Send()
        {
            if (!_pendingMessages.TryDequeue(out Message? message))
                return false;
            SendMessage(message);
            return true;
        }

        private void SendMessage(Message message)
        {
            try
            {
                _subscriber(message);
            }
            catch (Exception)
            {
            }
        }
    }
}
