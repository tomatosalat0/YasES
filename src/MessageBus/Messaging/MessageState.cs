namespace MessageBus.Messaging
{
    public enum MessageState
    {
        /// <summary>
        /// The initial state of the message.
        /// </summary>
        Initial,

        /// <summary>
        /// <see cref="IMessageOperation.Ack"/> has been called.
        /// </summary>
        Acknowledged,

        /// <summary>
        /// <see cref="IMessageOperation.Nack"/> has been called.
        /// </summary>
        NotAcknowledged
    }
}
