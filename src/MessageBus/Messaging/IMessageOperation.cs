namespace MessageBus.Messaging
{
    public interface IMessageOperation
    {
        /// <summary>
        /// Gets the state of the message.
        /// </summary>
        MessageState State { get; }

        /// <summary>
        /// Indicates that the message has been processed.
        /// The message won't get send again.
        /// </summary>
        void Ack();

        /// <summary>
        /// Indicates that the message has not been processed.
        /// The message won't get send again.
        /// </summary>
        void Nack();
    }
}
