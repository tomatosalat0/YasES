using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using MessageBus.Messaging.InProcess.Messages;

namespace MessageBus.Messaging.InProcess.Channels
{
    internal class ChannelSubscription : IDisposable
    {
        private readonly ConcurrentQueue<Message> _messages = new ConcurrentQueue<Message>();
        private readonly Action<Message> _execute;
        private readonly Action<ChannelSubscription> _onComplete;
        private readonly Func<Message?> _adHocFetch;
        private bool _disposedValue;

        public ChannelSubscription(
            Action<Message> execute,
            Action<ChannelSubscription> onComplete,
            Func<Message?> adHocFetch)
        {
            _execute = execute;
            _onComplete = onComplete;
            _adHocFetch = adHocFetch;
            DoWorkAction = WorkAction;
        }

        public bool HasMessages => !_messages.IsEmpty;

        public bool IsActive => !_disposedValue;

        public Action DoWorkAction { get; }

        public void Enqueue(Message value)
        {
            _messages.Enqueue(value);
        }

        private void WorkAction()
        {
            try
            {
                SendUntilTimeout();
            }
            finally
            {
                _onComplete(this);
            }
        }

        private void SendUntilTimeout()
        {
            if (_disposedValue)
                return;

            TimeSpan timeout = TimeSpan.FromMilliseconds(200);
            Stopwatch watch = Stopwatch.StartNew();
            int callCount = 0;
            while (++callCount < 100 && watch.Elapsed < timeout && SendOneMessage())
            {
            }
        }

        private bool SendOneMessage()
        {
            if (_disposedValue)
                return false;

            if (_messages.TryDequeue(out var p))
            {
                _execute(p);
                return true;
            } else
            if (TryGetAdHoc(out var n))
            {
                _execute(n);
                return true;
            }

            return false;
        }

        private bool TryGetAdHoc([NotNullWhen(true)] out Message? result)
        {
            result = _adHocFetch();
            return result is not null;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
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
