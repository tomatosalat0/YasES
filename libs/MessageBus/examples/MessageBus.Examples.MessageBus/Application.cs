using System;
using MessageBus.Examples.MessageBus.Logging;
using MessageBus.Examples.MessageBus.Weather;

namespace MessageBus.Examples.MessageBus
{
    public class Application : IDisposable
    {
        private readonly MessageBrokerMessageBus _eventBus;
        private bool _disposedValue;

        public Application()
        {
            _eventBus = new MessageBrokerMessageBus(MemoryMessageBrokerBuilder.InProcessBroker(), NoExceptionNotification.Instance);

            RegisterCommandHandlers();
            RegisterQueryHandlers();
            RegisterDefaultEventHandlers();
        }

        public IMessageBusHandler Handler => _eventBus;

        public IMessageBusPublishing Publish => _eventBus;

        private void RegisterCommandHandlers()
        {
            _eventBus.RegisterCommandHandler(new LogMessageCommandHandler(_eventBus));
        }

        private void RegisterQueryHandlers()
        {
            _eventBus.RegisterQueryHandler(new WttrInQueryHandler());
        }

        private void RegisterDefaultEventHandlers()
        {
            _eventBus.RegisterEventHandler(new CreatedMessageToConsoleHandler());
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _eventBus.Dispose();
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
