namespace YasES.Plugins.Messaging
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

    public interface IMessage : IMessageOperation
    {
        /// <summary>
        /// The payload of the message.
        /// </summary>
        object Payload { get; }
    }

    public interface IMessage<out T> : IMessageOperation
    {
        /// <summary>
        /// The payload of the message.
        /// </summary>
        T Payload { get; }
    }
}
