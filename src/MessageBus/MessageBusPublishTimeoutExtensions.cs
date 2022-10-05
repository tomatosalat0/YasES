using System;
using System.Threading;
using System.Threading.Tasks;

namespace MessageBus
{
    public static class MessageBusPublishTimeoutExtensions
    {
        /// <summary>synchronious 
        /// Executes the provided <paramref name="command"/> and waits for its execution. If the execution took longer than
        /// the provided <paramref name="timeout"/>, an <see cref="OperationCanceledException"/> will get thrown.
        /// See <see cref="IMessageBusPublishing.FireCommandAndWait{TCommand}(TCommand, CancellationToken)"/> for details.
        /// </summary>
        /// <exception cref="OperationCanceledException">Is thrown if the command wasn't handled within the specified <paramref name="timeout"/>.</exception>
        public static async Task FireCommandAndWait<TCommand>(this IMessageBusPublishing messageBus, TCommand command, TimeSpan timeout)
            where TCommand : IMessageCommand
        {
            using CancellationTokenSource timeoutTokenSource = new CancellationTokenSource(timeout);
            await messageBus.FireCommandAndWait(command, timeoutTokenSource.Token);
        }

        /// <summary>
        /// Executes the provided <paramref name="query"/> and waits for its return value. If the execution took longer than
        /// the provided <paramref name="timeout"/>, an <see cref="OperationCanceledException"/> will get thrown.
        /// See <see cref="IMessageBusPublishing.FireQuery{TQuery, TQueryResult}(TQuery, CancellationToken)"/> for details.
        /// </summary>
        /// <exception cref="OperationCanceledException">Is thrown if the command wasn't handled within the specified <paramref name="timeout"/>.</exception>
        public static async Task<TQueryResult> FireQuery<TQuery, TQueryResult>(this IMessageBusPublishing messageBus, TQuery query, TimeSpan timeout)
            where TQuery : IMessageQuery<TQueryResult>
            where TQueryResult : IMessageQueryResult
        {
            using CancellationTokenSource timeoutTokenSource = new CancellationTokenSource(timeout);
            return await messageBus.FireQuery<TQuery, TQueryResult>(query, timeoutTokenSource.Token);
        }

        /// <summary>
        /// Executes the provided <paramref name="rpcParameter"/> and waits for its return value. If the execution took longer than
        /// the provided <paramref name="timeout"/>, an <see cref="OperationCanceledException"/> will get thrown.
        /// See <see cref="IMessageBusPublishing.FireRpc{TRpc, TRpcResult}(TRpc, CancellationToken)"/> for details.
        /// </summary>
        /// <exception cref="OperationCanceledException">Is thrown if the command wasn't handled within the specified <paramref name="timeout"/>.</exception>
        public static async Task<TRpcResult> FireRpc<TRpc, TRpcResult>(this IMessageBusPublishing messageBus, TRpc rpcParameter, TimeSpan timeout)
            where TRpc : IMessageRpc<TRpcResult>
            where TRpcResult : IMessageRpcResult
        {
            using CancellationTokenSource timeoutTokenSource = new CancellationTokenSource(timeout);
            return await messageBus.FireRpc<TRpc, TRpcResult>(rpcParameter, timeoutTokenSource.Token);
        }
    }
}
