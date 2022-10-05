using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using MessageBus.Decorators;

namespace MessageBus
{
    [ExcludeFromCodeCoverage]
    public static class NonBlockingMessageHandlerExtensions
    {
        /// <summary>
        /// Wraps the provided <paramref name="handler"/> with another handler which will execute the
        /// <see cref="IAsyncMessageCommandHandler{TEvent}.HandleAsync(TEvent)"/> within its own task. This
        /// means that multiple threads will execute the wrapped handler simultaniously. Only use this
        /// method if you are sure that the wrapped handler is thread safe and stateless.
        /// </summary>
        /// <remarks>If the provided <paramref name="handler"/> implements <see cref="ISubscriptionAwareHandler"/>, the result
        /// will implement that interface as well.</remarks>
        public static IAsyncMessageCommandHandler<TCommand> WithParallelExecution<TCommand>(this IAsyncMessageCommandHandler<TCommand> handler)
            where TCommand : IMessageCommand
        {
            return new NonBlockingAsyncCommandHandler<TCommand>(handler);
        }

        /// <summary>
        /// Wraps the provided <paramref name="handler"/> with another handler which will execute the
        /// <see cref="IMessageCommandHandler{TEvent}.Handle(TEvent)"/> within its own task. This
        /// means that multiple threads will execute the wrapped handler simultaniously. Only use this
        /// method if you are sure that the wrapped handler is thread safe and stateless.
        /// </summary>
        /// <remarks>If the provided <paramref name="handler"/> implements <see cref="ISubscriptionAwareHandler"/>, the result
        /// will implement that interface as well.</remarks>
        public static IMessageCommandHandler<TCommand> WithParallelExecution<TCommand>(this IMessageCommandHandler<TCommand> handler)
            where TCommand : IMessageCommand
        {
            return new NonBlockingCommandHandler<TCommand>(handler);
        }

        /// <summary>
        /// Wraps the provided <paramref name="handler"/> with another handler which will execute the
        /// <see cref="IAsyncMessageEventHandler{TEvent}.HandleAsync(TEvent)"/> within its own task. This
        /// means that multiple threads will execute the wrapped handler simultaniously. Only use this
        /// method if you are sure that the wrapped handler is thread safe and stateless.
        /// </summary>
        /// <remarks>If the provided <paramref name="handler"/> implements <see cref="ISubscriptionAwareHandler"/>, the result
        /// will implement that interface as well.</remarks>
        public static IAsyncMessageEventHandler<TEvent> WithParallelExecution<TEvent>(this IAsyncMessageEventHandler<TEvent> handler)
            where TEvent : IMessageEvent
        {
            return new NonBlockingAsyncEventHandler<TEvent>(handler);
        }

        /// <summary>
        /// Wraps the provided <paramref name="handler"/> with another handler which will execute the
        /// <see cref="IMessageEventHandler{TEvent}.Handle(TEvent)"/> within its own task. This
        /// means that multiple threads will execute the wrapped handler simultaniously. Only use this
        /// method if you are sure that the wrapped handler is thread safe and stateless.
        /// </summary>
        /// <remarks>If the provided <paramref name="handler"/> implements <see cref="ISubscriptionAwareHandler"/>, the result
        /// will implement that interface as well.</remarks>
        public static IMessageEventHandler<TEvent> WithParallelExecution<TEvent>(this IMessageEventHandler<TEvent> handler)
            where TEvent : IMessageEvent
        {
            return new NonBlockingEventHandler<TEvent>(handler);
        }

        private class NonBlockingAsyncCommandHandler<TCommand> : DecoratedAsyncCommandHandler<TCommand>
            where TCommand : IMessageCommand
        {
            public NonBlockingAsyncCommandHandler(IAsyncMessageCommandHandler<TCommand> inner)
                : base(inner)
            {
            }

            public override Task HandleAsync(TCommand command)
            {
                Task.Run(async () => await base.HandleAsync(command));
                return Task.CompletedTask;
            }
        }

        private class NonBlockingCommandHandler<TCommand> : DecoratedCommandHandler<TCommand>
            where TCommand : IMessageCommand
        {
            public NonBlockingCommandHandler(IMessageCommandHandler<TCommand> inner)
                : base(inner)
            {
            }

            public override void Handle(TCommand command)
            {
                Task.Run(() => base.Handle(command));
            }
        }

        private class NonBlockingAsyncEventHandler<TEvent> : DecoratedAsyncEventHandler<TEvent>
            where TEvent : IMessageEvent
        {
            public NonBlockingAsyncEventHandler(IAsyncMessageEventHandler<TEvent> inner)
                : base(inner)
            {
            }

            public override Task HandleAsync(TEvent command)
            {
                Task.Run(async () => await base.HandleAsync(command));
                return Task.CompletedTask;
            }
        }

        private class NonBlockingEventHandler<TEvent> : DecoratedEventHandler<TEvent>
            where TEvent : IMessageEvent
        {
            public NonBlockingEventHandler(IMessageEventHandler<TEvent> inner)
                : base(inner)
            {
            }

            public override void Handle(TEvent command)
            {
                Task.Run(() => base.Handle(command));
            }
        }
    }
}
