using System;
using System.Threading.Tasks;

namespace MessageBus
{
    public static class MessageBusDelegateExtensions
    {
        public static IDisposable RegisterEventDelegate<TEvent>(this IMessageBusHandler subscriptionHandler, Action<TEvent> eventHandler)
            where TEvent : IMessageEvent
        {
            return subscriptionHandler.RegisterEventHandler(new DelegateEventHandler<TEvent>(eventHandler));
        }

        public static IDisposable RegisterEventDelegateAsync<TEvent>(this IMessageBusHandler subscriptionHandler, Func<TEvent, Task> eventHandler)
            where TEvent : IMessageEvent
        {
            return subscriptionHandler.RegisterEventHandler(new AsyncDelegateEventHandler<TEvent>(eventHandler));
        }

        public static IDisposable RegisterQueryDelegate<TQuery, TQueryResult>(this IMessageBusHandler subscriptionHandler, Func<TQuery, TQueryResult> queryHandler)
            where TQuery : IMessageQuery<TQueryResult>
            where TQueryResult : IMessageQueryResult
        {
            return subscriptionHandler.RegisterQueryHandler(new QueryHandler<TQuery, TQueryResult>(queryHandler));
        }

        public static IDisposable RegisterQueryDelegateAsync<TQuery, TQueryResult>(this IMessageBusHandler subscriptionHandler, Func<TQuery, Task<TQueryResult>> queryHandler)
            where TQuery : IMessageQuery<TQueryResult>
            where TQueryResult : IMessageQueryResult
        {
            return subscriptionHandler.RegisterQueryHandler(new AsyncQueryHandler<TQuery, TQueryResult>(queryHandler));
        }

        public static IDisposable RegisterCommandDelegate<TCommand>(this IMessageBusHandler subscriptionHandler, Action<TCommand> commandHandler)
            where TCommand : IMessageCommand
        {
            return subscriptionHandler.RegisterCommandHandler(new CommandHandler<TCommand>(commandHandler));
        }

        public static IDisposable RegisterCommandDelegateAsync<TCommand>(this IMessageBusHandler subscriptionHandler, Func<TCommand, Task> commandHandler)
            where TCommand : IMessageCommand
        {
            return subscriptionHandler.RegisterCommandHandler(new AsyncCommandHandler<TCommand>(commandHandler));
        }

        public static IDisposable RegisterRpcDelegate<TRpc, TRpcResult>(this IMessageBusHandler subscriptionHandler, Func<TRpc, TRpcResult> queryHandler)
            where TRpc : IMessageRpc<TRpcResult>
            where TRpcResult : IMessageRpcResult
        {
            return subscriptionHandler.RegisterRpcHandler(new RpcHandler<TRpc, TRpcResult>(queryHandler));
        }

        public static IDisposable RegisterRpcDelegateAsync<TRpc, TRpcResult>(this IMessageBusHandler subscriptionHandler, Func<TRpc, Task<TRpcResult>> queryHandler)
            where TRpc : IMessageRpc<TRpcResult>
            where TRpcResult : IMessageRpcResult
        {
            return subscriptionHandler.RegisterRpcHandler(new AsyncRpcHandler<TRpc, TRpcResult>(queryHandler));
        }

        private class DelegateEventHandler<TEvent> : IMessageEventHandler<TEvent> where TEvent : IMessageEvent
        {
            private readonly Action<TEvent> _handler;

            public DelegateEventHandler(Action<TEvent> handler)
            {
                _handler = handler;
            }

            public void Handle(TEvent @event)
            {
                _handler(@event);
            }
        }

        private class AsyncDelegateEventHandler<TEvent> : IAsyncMessageEventHandler<TEvent>
            where TEvent : IMessageEvent
        {
            private readonly Func<TEvent, Task> _handler;

            public AsyncDelegateEventHandler(Func<TEvent, Task> handler)
            {
                _handler = handler;
            }

            public Task HandleAsync(TEvent @event)
            {
                return _handler(@event);
            }
        }

        private class QueryHandler<TQuery, TQueryResult> : IMessageQueryHandler<TQuery, TQueryResult>
            where TQuery : IMessageQuery<TQueryResult>
            where TQueryResult : IMessageQueryResult
        {
            private readonly Func<TQuery, TQueryResult> _handler;

            public QueryHandler(Func<TQuery, TQueryResult> handler)
            {
                _handler = handler;
            }

            public TQueryResult Handle(TQuery query)
            {
                return _handler(query);
            }
        }

        private class AsyncQueryHandler<TQuery, TQueryResult> : IAsyncMessageQueryHandler<TQuery, TQueryResult>
            where TQuery : IMessageQuery<TQueryResult>
            where TQueryResult : IMessageQueryResult
        {
            private readonly Func<TQuery, Task<TQueryResult>> _handler;

            public AsyncQueryHandler(Func<TQuery, Task<TQueryResult>> handler)
            {
                _handler = handler;
            }

            public Task<TQueryResult> HandleAsync(TQuery query)
            {
                return _handler(query);
            }
        }

        private class RpcHandler<TRcp, TRpcResult> : IMessageRpcHandler<TRcp, TRpcResult>
            where TRcp : IMessageRpc<TRpcResult>
            where TRpcResult : IMessageRpcResult
        {
            private readonly Func<TRcp, TRpcResult> _handler;

            public RpcHandler(Func<TRcp, TRpcResult> handler)
            {
                _handler = handler;
            }

            public TRpcResult Handle(TRcp query)
            {
                return _handler(query);
            }
        }

        private class AsyncRpcHandler<TRpc, TRpcResult> : IAsyncMessageRpcHandler<TRpc, TRpcResult>
            where TRpc : IMessageRpc<TRpcResult>
            where TRpcResult : IMessageRpcResult
        {
            private readonly Func<TRpc, Task<TRpcResult>> _handler;

            public AsyncRpcHandler(Func<TRpc, Task<TRpcResult>> handler)
            {
                _handler = handler;
            }

            public Task<TRpcResult> HandleAsync(TRpc query)
            {
                return _handler(query);
            }
        }

        private class CommandHandler<TCommand> : IMessageCommandHandler<TCommand>
            where TCommand : IMessageCommand
        {
            private readonly Action<TCommand> _handler;

            public CommandHandler(Action<TCommand> handler)
            {
                _handler = handler;
            }

            public void Handle(TCommand query)
            {
                _handler(query);
            }
        }

        private class AsyncCommandHandler<TCommand> : IAsyncMessageCommandHandler<TCommand>
            where TCommand : IMessageCommand
        {
            private readonly Func<TCommand, Task> _handler;

            public AsyncCommandHandler(Func<TCommand, Task> handler)
            {
                _handler = handler;
            }

            public Task HandleAsync(TCommand query)
            {
                return _handler(query);
            }
        }
    }
}
