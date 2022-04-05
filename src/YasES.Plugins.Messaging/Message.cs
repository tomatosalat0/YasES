using System;

namespace YasES.Plugins.Messaging
{
    internal class MessageStateContainer : IMessageOperation
    {
        public MessageState State { get; private set; }

        public void Ack()
        {
            if (State == MessageState.NotAcknowledged)
                throw new InvalidOperationException($"The message is already not acknowledged.");
            State = MessageState.Acknowledged;
        }

        public void Nack()
        {
            if (State == MessageState.Acknowledged)
                throw new InvalidOperationException($"The message is already acknowledged.");
            State = MessageState.NotAcknowledged;
        }
    }

    internal abstract class MessageBase : IMessageOperation
    {
        internal readonly MessageStateContainer StateContainer;

        protected MessageBase() : this(new MessageStateContainer())
        {
        }

        protected MessageBase(MessageStateContainer state)
        {
            StateContainer = state;
        }

        public MessageState State => StateContainer.State;

        public void Ack() => StateContainer.Ack();

        public void Nack() => StateContainer.Nack();
    }

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
