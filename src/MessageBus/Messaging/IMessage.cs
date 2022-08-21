namespace MessageBus.Messaging
{
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
