using System;

namespace MessageBus
{
    public sealed class NoExceptionNotification : IExceptionNotification
    {
        private NoExceptionNotification()
        {
        }

        public static NoExceptionNotification Instance { get; } = new NoExceptionNotification();

        public void NotifyException(MessageId messageId, object message, Exception exception)
        {
            // intentionally left blank.
        }
    }
}
