namespace MessageBus.Decorators
{
    public abstract class DecoratedRpcHandler<TRpc, TRpcResult> : DecoratedSubscriptionAwareHandler, IMessageRpcHandler<TRpc, TRpcResult>
        where TRpc : IMessageRpc<TRpcResult>
        where TRpcResult : IMessageRpcResult
    {
        protected IMessageRpcHandler<TRpc, TRpcResult> Inner { get; }

        protected DecoratedRpcHandler(IMessageRpcHandler<TRpc, TRpcResult> inner)
            : base(inner)
        {
            Inner = inner;
        }

        public TRpcResult Handle(TRpc query)
        {
            return Inner.Handle(query);
        }
    }
}
