using System;
using System.Threading.Tasks;

namespace MessageBus
{
    public interface IMessageQuery<out TQueryResult> : IBusMessage
            where TQueryResult : IMessageQueryResult
    {
    }

    public interface IMessageQueryResult : IBusMessage
    {
        public sealed class FailureResult : IMessageQueryResult
        {
            public FailureResult(MessageId messageId, Exception exception)
            {
                MessageId = messageId;
                Exception = exception ?? throw new ArgumentNullException(nameof(exception));
            }

            public MessageId MessageId { get; }

            public Exception Exception { get; }
        }
    }

    public interface IMessageQueryHandler<in TQuery, out TQueryResult>
        where TQuery : IMessageQuery<TQueryResult>
        where TQueryResult : IMessageQueryResult
    {
        TQueryResult Handle(TQuery query);
    }

    public interface IAsyncMessageQueryHandler<in TQuery, TQueryResult>
        where TQuery : IMessageQuery<TQueryResult>
        where TQueryResult : IMessageQueryResult
    {
        Task<TQueryResult> HandleAsync(TQuery query);
    }
}
