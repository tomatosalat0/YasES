using System;

namespace MessageBus
{
    public sealed class NullExceptionNotification : IExceptionNotification
    {
        private NullExceptionNotification()
        {
        }

        public static NullExceptionNotification Instance { get; } = new NullExceptionNotification();

        public void NotifyException(MessageId messageId, object message, Exception exception)
        {
            // intentionally left blank.
        }
    }
}
