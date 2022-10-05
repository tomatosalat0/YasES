using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using MessageBus.Messaging;

namespace MessageBus
{
    /// <summary>
    /// This part of the event bus is responsible for the <see cref="IMessageBusPublishing"/>
    /// implementation.
    /// </summary>
    public partial class MessageBrokerMessageBus : IMessageBusPublishing, IDisposable
    {
        /// <inheritdoc/>
        public Task FireEvent<TEvent>(TEvent @event)
            where TEvent : IMessageEvent
        {
            ThrowDisposed();
            return _broker.Publish(@event, GetTopicNameFromType(typeof(TEvent)));
        }

        /// <inheritdoc/>
        public Task FireCommand<TCommand>(TCommand command)
            where TCommand : IMessageCommand
        {
            ThrowDisposed();
            return _broker.Publish(command, GetTopicNameFromType(typeof(TCommand)));
        }

        /// <inheritdoc/>
        public Task FireCommandAndWait<TCommand>(TCommand command, CancellationToken cancellationToken)
            where TCommand : IMessageCommand
        {
            ThrowDisposed();

            TopicName fireTopic = GetTopicNameFromType(typeof(TCommand));
            TopicName outcomeTopic = GetOutcomeTopicName(command);

            return FireAndAwait<TCommand, MessageCommandOutcome, MessageCommandOutcome>(
                fireTopic,
                outcomeTopic,
                command,
                (outcome) =>
                {
                    if (outcome.Exception is not null)
                    {
                        NotifyException(outcome.MessageId, command, outcome.Exception);
                        ExceptionDispatchInfo.Capture(outcome.Exception).Throw();
                    }

                    return outcome;
                },
                cancellationToken
            );
        }

        /// <inheritdoc/>
        public Task<TQueryResult> FireQuery<TQuery, TQueryResult>(TQuery query, CancellationToken cancellationToken)
            where TQuery : IMessageQuery<TQueryResult>
            where TQueryResult : IMessageQueryResult
        {
            ThrowDisposed();

            TopicName fireTopic = GetTopicNameFromType(typeof(TQuery));
            TopicName outcomeTopic = GetOutcomeTopicName(query);

            return FireAndAwait<TQuery, TQueryResult, IMessageQueryResult>(
                fireTopic,
                outcomeTopic,
                query,
                (outcome) =>
                {
                    if (outcome is IMessageQueryResult.FailureResult failureResult)
                    {
                        NotifyException(failureResult.MessageId, query, failureResult.Exception);
                        ExceptionDispatchInfo.Capture(failureResult.Exception).Throw();
                    }

                    return (TQueryResult)outcome;
                },
                cancellationToken
            );
        }

        /// <inheritdoc/>
        public Task<TRpcResult> FireRpc<TRpc, TRpcResult>(TRpc rpcParameter, CancellationToken cancellationToken)
            where TRpc : IMessageRpc<TRpcResult>
            where TRpcResult : IMessageRpcResult
        {
            ThrowDisposed();

            TopicName fireTopic = GetTopicNameFromType(typeof(TRpc));
            TopicName outcomeTopic = GetOutcomeTopicName(rpcParameter);

            return FireAndAwait<TRpc, TRpcResult, IMessageRpcResult>(
                fireTopic,
                outcomeTopic,
                rpcParameter,
                (outcome) =>
                {
                    if (outcome is IMessageRpcResult.FailureResult failureResult)
                    {
                        NotifyException(failureResult.MessageId, rpcParameter, failureResult.Exception);
                        ExceptionDispatchInfo.Capture(failureResult.Exception).Throw();
                    }

                    return (TRpcResult)outcome;
                },
                cancellationToken
            );
        }

        private async Task<TQueryResult> FireAndAwait<TRequest, TQueryResult, TResponse>(
            TopicName fireTopic,
            TopicName resultTopic,
            TRequest request,
            Func<TResponse, TQueryResult> transformResult,
            CancellationToken cancellationToken)
            where TRequest : IHasMessageId
            where TResponse : IHasMessageId
        {
            var channel = System.Threading.Channels.Channel.CreateUnbounded<TResponse>(new System.Threading.Channels.UnboundedChannelOptions()
            {
                AllowSynchronousContinuations = true,
                SingleReader = true,
                SingleWriter = true
            });

            using var subscription = RegisterCompleteHandler<TResponse>(resultTopic, (o) => channel.Writer.TryWrite(o));

            await _broker.Publish(request, fireTopic);
            await foreach (var p in channel.Reader.ReadAllAsync(cancellationToken))
            {
                if (p.MessageId == request.MessageId)
                    return transformResult(p);
            }

            throw new InvalidOperationException($"Did not receive any response");
        }

        private IDisposable RegisterCompleteHandler<TResponse>(TopicName topic, Action<TResponse> onReceived)
            where TResponse : notnull
        {
            if (onReceived is null) throw new ArgumentNullException(nameof(onReceived));

            IDisposable result = _broker
                .Events(topic, EventsOptions.Temporary)
                .Subscribe<TResponse>(m =>
                {
                    m.Ack();
                    onReceived(m.Payload);
                });

            return result;
        }
    }
}
