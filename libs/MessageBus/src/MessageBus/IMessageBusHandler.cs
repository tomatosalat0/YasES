using System;

namespace MessageBus
{
    public interface IMessageBusHandler
    {
        /// <summary>
        /// Registers the provided <paramref name="handler"/> as a receiver for the
        /// events defined by <typeparamref name="TEvent"/>. The returned value
        /// is the subscription handle. When it gets disposed, the handler won't get any
        /// new message. If <paramref name="handler"/> implements <see cref="ISubscriptionAwareHandler"/>,
        /// the handler will get the returned handle automatically.
        /// </summary>
        IDisposable RegisterEventHandler<TEvent>(IMessageEventHandler<TEvent> handler)
            where TEvent : IMessageEvent;

        /// <summary>
        /// Registers the provided async <paramref name="handler"/> as a receiver for the
        /// events defined by <typeparamref name="TEvent"/>. The returned value
        /// is the subscription handle. When it gets disposed, the handler won't get any
        /// new message. If <paramref name="handler"/> implements <see cref="ISubscriptionAwareHandler"/>,
        /// the handler will get the returned handle automatically.
        /// </summary>
        IDisposable RegisterEventHandler<TEvent>(IAsyncMessageEventHandler<TEvent> handler)
            where TEvent : IMessageEvent;

        /// <summary>
        /// Registers the provided <paramref name="handler"/> as a receiver for the
        /// commands defined by <typeparamref name="TCommand"/>. The returned value
        /// is the subscription handle. When it gets disposed, the handler won't get any
        /// new message. If <paramref name="handler"/> implements <see cref="ISubscriptionAwareHandler"/>,
        /// the handler will get the returned handle automatically.
        /// </summary>
        IDisposable RegisterCommandHandler<TCommand>(IMessageCommandHandler<TCommand> handler)
            where TCommand : IMessageCommand;

        /// <summary>
        /// Registers the provided async <paramref name="handler"/> as a receiver for the
        /// commands defined by <typeparamref name="TCommand"/>. The returned value
        /// is the subscription handle. When it gets disposed, the handler won't get any
        /// new message. If <paramref name="handler"/> implements <see cref="ISubscriptionAwareHandler"/>,
        /// the handler will get the returned handle automatically.
        /// </summary>
        IDisposable RegisterCommandHandler<TCommand>(IAsyncMessageCommandHandler<TCommand> handler)
            where TCommand : IMessageCommand;

        /// <summary>
        /// Registers the provided <paramref name="handler"/> as a receiver for the
        /// queries defined by <typeparamref name="TQuery"/>. The returned value
        /// is the subscription handle. When it gets disposed, the handler won't get any
        /// new message. If <paramref name="handler"/> implements <see cref="ISubscriptionAwareHandler"/>,
        /// the handler will get the returned handle automatically.
        /// </summary>
        IDisposable RegisterQueryHandler<TQuery, TQueryResult>(IMessageQueryHandler<TQuery, TQueryResult> handler)
            where TQuery : IMessageQuery<TQueryResult>
            where TQueryResult : IMessageQueryResult;

        /// <summary>
        /// Registers the provided async <paramref name="handler"/> as a receiver for the
        /// queries defined by <typeparamref name="TQuery"/>. The returned value
        /// is the subscription handle. When it gets disposed, the handler won't get any
        /// new message. If <paramref name="handler"/> implements <see cref="ISubscriptionAwareHandler"/>,
        /// the handler will get the returned handle automatically.
        /// </summary>
        IDisposable RegisterQueryHandler<TQuery, TQueryResult>(IAsyncMessageQueryHandler<TQuery, TQueryResult> handler)
            where TQuery : IMessageQuery<TQueryResult>
            where TQueryResult : IMessageQueryResult;

        /// <summary>
        /// Registers the provided <paramref name="handler"/> as a receiver for remote procedure
        /// calls defined by <typeparamref name="TRpc"/>. The returned value is
        /// the subscription handler. When it gets disposed, the handler won't get any
        /// new message. If <paramref name="handler"/> implements <see cref="ISubscriptionAwareHandler"/>,
        /// the handler will get the returned handle automatically.
        /// </summary>
        IDisposable RegisterRpcHandler<TRpc, TRpcResult>(IMessageRpcHandler<TRpc, TRpcResult> handler)
            where TRpc : IMessageRpc<TRpcResult>
            where TRpcResult : IMessageRpcResult;

        /// <summary>
        /// Registers the provided async <paramref name="handler"/> as a receiver for remote procedure
        /// calls defined by <typeparamref name="TRpc"/>. The returned value is
        /// the subscription handler. When it gets disposed, the handler won't get any
        /// new message. If <paramref name="handler"/> implements <see cref="ISubscriptionAwareHandler"/>,
        /// the handler will get the returned handle automatically.
        /// </summary>
        IDisposable RegisterRpcHandler<TRpc, TRpcResult>(IAsyncMessageRpcHandler<TRpc, TRpcResult> handler)
            where TRpc : IMessageRpc<TRpcResult>
            where TRpcResult : IMessageRpcResult;
    }
}
