using System.Threading.Tasks;

namespace MessageBus.Decorators
{
    public abstract class DecoratedAsyncCommandHandler<TCommand> : DecoratedSubscriptionAwareHandler, IAsyncMessageCommandHandler<TCommand>
        where TCommand : IMessageCommand
    {
        private readonly IAsyncMessageCommandHandler<TCommand> _inner;

        protected DecoratedAsyncCommandHandler(IAsyncMessageCommandHandler<TCommand> inner)
            : base(inner)
        {
            _inner = inner;
        }

        public virtual Task HandleAsync(TCommand command)
        {
            return _inner.HandleAsync(command);
        }
    }
}
