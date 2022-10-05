using System;

namespace MessageBus.Messaging.InProcess.Messages
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
}
