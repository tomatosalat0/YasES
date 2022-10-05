using System;
using System.Threading;
using System.Threading.Tasks;

namespace MessageBus
{
    public interface IMessageBusPublishing : IDisposable
    {
        /// <summary>
        /// Fires the provided <paramref name="event"/> to all handlers which are
        /// currently subscribed to the provided event type. This method
        /// will return immediately.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is thrown if the object is already disposed.</exception>
        Task FireEvent<TEvent>(TEvent @event)
            where TEvent : IMessageEvent;

        /// <summary>
        /// Fires the provided <paramref name="command"/> to all handlers which are
        /// currently subscribed to the provided event type.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is thrown if the object is already disposed.</exception>
        Task FireCommand<TCommand>(TCommand command)
            where TCommand : IMessageCommand;

        /// <summary>
        /// Fires the provided <paramref name="command"/> to one of the currently subscribed handlers for
        /// the provided event type. This method will not return until the handler has sent back a response. If no handler for the
        /// provided <paramref name="command"/> is available, this method will never return.
        /// </summary>
        /// <remarks>Normally you should not wait for a command result. If the handler is currently not available, the command
        /// might get executed even after the <paramref name="cancellationToken"/> has been cancelled. If no cancellation token is set,
        /// the call might return in the far future. If possible, use <see cref="FireCommand{TCommand}(TCommand)"/>, fire events
        /// within the handler itself and continue with the workflow after receiving these events.</remarks>
        /// <exception cref="OperationCanceledException">Is thrown if the provided <paramref name="cancellationToken"/>
        /// is cancelled.</exception>
        /// <exception cref="ObjectDisposedException">Is thrown if the object is already disposed.</exception>
        /// <exception cref="Exception">Exceptions which happened within the handler will get rethrown.</exception>
        Task FireCommandAndWait<TCommand>(TCommand command, CancellationToken cancellationToken)
            where TCommand : IMessageCommand;

        /// <summary>
        /// Fires the provided <paramref name="query"/> to one of the currently subscribed handlers for
        /// the provided event type. This method will not return until the handler has sent back a response. If no handler for the
        /// provided <paramref name="query"/> is available, this method will never return.
        /// </summary>
        /// <exception cref="OperationCanceledException">Is thrown if the provided <paramref name="cancellationToken"/>
        /// is cancelled.</exception>
        /// <exception cref="ObjectDisposedException">Is thrown if the object is already disposed.</exception>
        /// <exception cref="Exception">Exceptions which happened within the handler will get rethrown.</exception>
        Task<TQueryResult> FireQuery<TQuery, TQueryResult>(TQuery query, CancellationToken cancellationToken)
            where TQuery : IMessageQuery<TQueryResult>
            where TQueryResult : IMessageQueryResult;

        /// <summary>
        /// Executes the remote procedure with the provided <paramref name="rpcParameter"/> to one of the currently
        /// subscribed handlers. This method will not return until the handler has sent back a response. If no handler for the
        /// provided <paramref name="rpcParameter"/> is available, this method will never return.
        /// </summary>
        /// <remarks>While a RPC might look identical to a query, it is semantically different. A query should not change any
        /// state while an RPC doesn't have this constraint. However, if you want to go with asynchronious communication,
        /// you should avoid RPC calls and replace them with Commands.</remarks>
        /// <exception cref="OperationCanceledException">Is thrown if the provided <paramref name="cancellationToken"/>
        /// is cancelled.</exception>
        /// <exception cref="ObjectDisposedException">Is thrown if the object is already disposed.</exception>
        /// <exception cref="Exception">Exceptions which happened within the handler will get rethrown.</exception>
        Task<TRpcResult> FireRpc<TRpc, TRpcResult>(TRpc rpcParameter, CancellationToken cancellationToken)
            where TRpc : IMessageRpc<TRpcResult>
            where TRpcResult : IMessageRpcResult;
    }
}
