namespace MessageBus.Messaging.InProcess.Messages
{
    internal abstract class MessageBase : IMessageOperation
    {
        private readonly MessageStateContainer _stateContainer;

        internal MessageStateContainer StateContainer => _stateContainer;

        protected MessageBase()
            : this(new MessageStateContainer())
        {
        }

        protected MessageBase(MessageStateContainer state)
        {
            _stateContainer = state;
        }

        public MessageState State => _stateContainer.State;

        public void Ack() => _stateContainer.Ack();

        public void Nack() => _stateContainer.Nack();
    }
}
