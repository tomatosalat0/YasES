namespace MessageBus.Decorators
{
    public abstract class DecoratedQueryHandler<TQuery, TQueryResult> : DecoratedSubscriptionAwareHandler, IMessageQueryHandler<TQuery, TQueryResult>
        where TQuery : IMessageQuery<TQueryResult>
        where TQueryResult : IMessageQueryResult
    {
        protected IMessageQueryHandler<TQuery, TQueryResult> Inner { get; }

        protected DecoratedQueryHandler(IMessageQueryHandler<TQuery, TQueryResult> inner)
            : base(inner)
        {
            Inner = inner;
        }

        public TQueryResult Handle(TQuery query)
        {
            return Inner.Handle(query);
        }
    }
}
