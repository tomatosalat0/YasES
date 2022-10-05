using System;

namespace MessageBus
{
    public interface IExceptionNotification
    {
        /// <summary>
        /// Gets called when an exception occured within any handler. The parameter <paramref name="messageId"/>
        /// is the correlation id of the message which got procssed. <paramref name="message"/> is the
        /// actual message which got processed. Normally this can be something implementing <see cref="IMessageQuery{TQueryResult}"/>,
        /// <see cref="IMessageEvent"/> or <see cref="IMessageCommand"/>. <paramref name="exception"/> is the exception
        /// which didn't get caught within the handler itself.
        /// </summary>
        /// <remarks>The notification process main purpose is to implement logging or a custom dead letter. Do not
        /// throw the provided exception.</remarks>
        void NotifyException(MessageId messageId, object message, Exception exception);
    }
}
