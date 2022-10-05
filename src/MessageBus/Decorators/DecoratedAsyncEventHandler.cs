using System.Threading.Tasks;

namespace MessageBus.Decorators
{
    public abstract class DecoratedAsyncEventHandler<TEvent> : DecoratedSubscriptionAwareHandler, IAsyncMessageEventHandler<TEvent>
        where TEvent : IMessageEvent
    {
        protected IAsyncMessageEventHandler<TEvent> Inner { get; }

        protected DecoratedAsyncEventHandler(IAsyncMessageEventHandler<TEvent> inner)
            : base(inner)
        {
            Inner = inner;
        }

        public virtual Task HandleAsync(TEvent command)
        {
            return Inner.HandleAsync(command);
        }
    }
}
