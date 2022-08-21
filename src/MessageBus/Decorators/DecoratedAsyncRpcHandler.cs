using System.Threading.Tasks;

namespace MessageBus.Decorators
{
    public abstract class DecoratedAsyncRpcHandler<TRpc, TRpcResult> : DecoratedSubscriptionAwareHandler, IAsyncMessageRpcHandler<TRpc, TRpcResult>
        where TRpc : IMessageRpc<TRpcResult>
        where TRpcResult : IMessageRpcResult
    {
        protected IAsyncMessageRpcHandler<TRpc, TRpcResult> Inner { get; }

        protected DecoratedAsyncRpcHandler(IAsyncMessageRpcHandler<TRpc, TRpcResult> inner)
            : base(inner)
        {
            Inner = inner;
        }

        public Task<TRpcResult> HandleAsync(TRpc query)
        {
            return Inner.HandleAsync(query);
        }
    }
}
