namespace MessageBus.Decorators
{
    public abstract class DecoratedCommandHandler<TCommand> : DecoratedSubscriptionAwareHandler, IMessageCommandHandler<TCommand>
        where TCommand : IMessageCommand
    {
        protected IMessageCommandHandler<TCommand> Inner { get; }

        protected DecoratedCommandHandler(IMessageCommandHandler<TCommand> inner)
            : base(inner)
        {
            Inner = inner;
        }

        public virtual void Handle(TCommand command)
        {
            Inner.Handle(command);
        }
    }
}
