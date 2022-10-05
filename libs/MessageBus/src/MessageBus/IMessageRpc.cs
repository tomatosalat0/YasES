using System;
using System.Threading.Tasks;

namespace MessageBus
{
    public interface IMessageRpc<out TRpcResult> : IBusMessage
            where TRpcResult : IMessageRpcResult
    {
    }

    public interface IMessageRpcResult : IBusMessage
    {
        public sealed class FailureResult : IMessageRpcResult
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

    public interface IMessageRpcHandler<in TRpc, out TRpcResult>
        where TRpc : IMessageRpc<TRpcResult>
        where TRpcResult : IMessageRpcResult
    {
        TRpcResult Handle(TRpc query);
    }

    public interface IAsyncMessageRpcHandler<in TRpc, TRpcResult>
        where TRpc : IMessageRpc<TRpcResult>
        where TRpcResult : IMessageRpcResult
    {
        Task<TRpcResult> HandleAsync(TRpc query);
    }
}
