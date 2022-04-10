namespace YasES.Plugins.Messaging
{
    internal class Message : MessageBase, IMessage
    {
        internal Message(object payload, MessageStateContainer state)
            : base(state)
        {
            Payload = payload;
        }

        public Message(object payload)
        {
            Payload = payload;
        }

        public object Payload { get; }
    }

    internal class Message<T> : MessageBase, IMessage<T>, IMessage
    {
        internal Message(T payload, MessageStateContainer state)
            : base(state)
        {
            Payload = payload;
        }

        public Message(T payload)
        {
            Payload = payload;
        }

        object IMessage.Payload => Payload!;

        public T Payload { get; }

        public static implicit operator Message(Message<T> message) => new Message(message.Payload!, message.StateContainer);
        public static implicit operator Message<T>(Message message) => new Message<T>((T)message.Payload, message.StateContainer);
    }
}
