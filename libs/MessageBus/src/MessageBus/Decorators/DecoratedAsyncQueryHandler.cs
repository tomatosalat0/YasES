using System.Threading.Tasks;

namespace MessageBus.Decorators
{
    public abstract class DecoratedAsyncQueryHandler<TQuery, TQueryResult> : DecoratedSubscriptionAwareHandler, IAsyncMessageQueryHandler<TQuery, TQueryResult>
        where TQuery : IMessageQuery<TQueryResult>
        where TQueryResult : IMessageQueryResult
    {
        protected IAsyncMessageQueryHandler<TQuery, TQueryResult> Inner { get; }

        protected DecoratedAsyncQueryHandler(IAsyncMessageQueryHandler<TQuery, TQueryResult> inner)
            : base(inner)
        {
            Inner = inner;
        }

        public Task<TQueryResult> HandleAsync(TQuery query)
        {
            return Inner.HandleAsync(query);
        }
    }
}
