using System;
using System.Threading.Tasks;

namespace MessageBus
{
    public interface IMessageCommand : IBusMessage
    {
    }

    public interface IMessageCommandHandler<in TCommand>
        where TCommand : IMessageCommand
    {
        void Handle(TCommand command);
    }

    public interface IAsyncMessageCommandHandler<in TCommand>
        where TCommand : IMessageCommand
    {
        Task HandleAsync(TCommand command);
    }

    public readonly struct MessageCommandOutcome : IBusMessage
    {
        private MessageCommandOutcome(MessageId messageId, Exception? exception)
        {
            MessageId = messageId;
            Exception = exception;
        }

        public static MessageCommandOutcome Success(MessageId id)
        {
            return new MessageCommandOutcome(id, null);
        }

        public static MessageCommandOutcome Failure(MessageId id, Exception exception)
        {
            return new MessageCommandOutcome(id, exception);
        }

        public MessageId MessageId { get; }

        internal Exception? Exception { get; }
    }
}
