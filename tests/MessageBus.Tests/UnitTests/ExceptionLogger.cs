using System;

namespace MessageBus.Tests.UnitTests
{
    internal class ExceptionLogger : IExceptionNotification
    {
        private readonly Action<MessageId, object, Exception> _handle;

        public ExceptionLogger(Action<MessageId, object, Exception> handle)
        {
            _handle = handle;
        }

        public void NotifyException(MessageId messageId, object message, Exception exception)
        {
            _handle(messageId, message, exception);
        }
    }
}
