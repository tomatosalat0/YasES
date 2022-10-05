using System;

namespace MessageBus.Examples.MessageBus.Logging
{
    public sealed class CreatedMessageToConsoleHandler : IMessageEventHandler<LogMessageCreated>, ISubscriptionAwareHandler, IDisposable
    {
        private readonly string _prefix;
        private IDisposable? _subscription;
        private bool _disposedValue;

        public CreatedMessageToConsoleHandler(string prefix)
        {
            _prefix = prefix;
        }

        public CreatedMessageToConsoleHandler()
            : this(string.Empty)
        {
        }

        void ISubscriptionAwareHandler.RegisterSubscription(IDisposable subscription)
        {
            if (_subscription is not null)
                throw new InvalidOperationException($"The handler is already active");
            _subscription = subscription;
        }

        public void Handle(LogMessageCreated @event)
        {
            Console.WriteLine("\t" + _prefix + @event.LogMessage);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _subscription?.Dispose();
                    _subscription = null;
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
