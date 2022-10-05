using System;
using System.Threading.Tasks;
using MessageBus.Messaging;

namespace MessageBus
{
    /// <summary>
    /// This part of the event bus is responsible for the <see cref="IMessageBusHandler"/>
    /// implementation.
    /// </summary>
    public partial class MessageBrokerMessageBus : IMessageBusHandler, IDisposable
    {
        /// <inheritdoc/>
        public IDisposable RegisterEventHandler<TEvent>(IMessageEventHandler<TEvent> handler)
            where TEvent : IMessageEvent
        {
            ThrowDisposed();
            return InnerRegisterEventHandler<TEvent, IMessageEventHandler<TEvent>>(
                handler,
                m =>
                {
                    handler.Handle(m.Payload);
                    return Task.CompletedTask;
                }
            );
        }

        /// <inheritdoc/>
        public IDisposable RegisterEventHandler<TEvent>(IAsyncMessageEventHandler<TEvent> handler)
            where TEvent : IMessageEvent
        {
            ThrowDisposed();
            return InnerRegisterEventHandler<TEvent, IAsyncMessageEventHandler<TEvent>>(
                handler,
                m => handler.HandleAsync(m.Payload)
            );
        }

        /// <inheritdoc/>
        public IDisposable RegisterCommandHandler<TCommand>(IMessageCommandHandler<TCommand> handler)
            where TCommand : IMessageCommand
        {
            ThrowDisposed();
            return InnerRegisterCommandHandler<TCommand, IMessageCommandHandler<TCommand>>(
                handler,
                m =>
                {
                    handler.Handle(m);
                    return Task.CompletedTask;
                }
            );
        }

        /// <inheritdoc/>
        public IDisposable RegisterCommandHandler<TCommand>(IAsyncMessageCommandHandler<TCommand> handler)
            where TCommand : IMessageCommand
        {
            ThrowDisposed();
            return InnerRegisterCommandHandler<TCommand, IAsyncMessageCommandHandler<TCommand>>(
                handler,
                m => handler.HandleAsync(m)
            );
        }

        /// <inheritdoc/>
        public IDisposable RegisterQueryHandler<TQuery, TQueryResult>(IMessageQueryHandler<TQuery, TQueryResult> handler)
            where TQuery : IMessageQuery<TQueryResult>
            where TQueryResult : IMessageQueryResult
        {
            ThrowDisposed();
            return InnerRegisterQueryHandler<TQuery, TQueryResult, IMessageQueryHandler<TQuery, TQueryResult>>(
                handler,
                m => handler.Handle(m)
            );
        }

        /// <inheritdoc/>
        public IDisposable RegisterQueryHandler<TQuery, TQueryResult>(IAsyncMessageQueryHandler<TQuery, TQueryResult> handler)
            where TQuery : IMessageQuery<TQueryResult>
            where TQueryResult : IMessageQueryResult
        {
            ThrowDisposed();
            return InnerRegisterQueryHandler<TQuery, TQueryResult, IAsyncMessageQueryHandler<TQuery, TQueryResult>>(
                handler,
                m => handler.HandleAsync(m).ConfigureAwait(false).GetAwaiter().GetResult()
            );
        }

        /// <inheritdoc/>
        public IDisposable RegisterRpcHandler<TRpc, TRpcResult>(IMessageRpcHandler<TRpc, TRpcResult> handler)
            where TRpc : IMessageRpc<TRpcResult>
            where TRpcResult : IMessageRpcResult
        {
            ThrowDisposed();
            return InnerRegisterRpcHandler<TRpc, TRpcResult, IMessageRpcHandler<TRpc, TRpcResult>>(
                handler,
                m => handler.Handle(m)
            );
        }

        /// <inheritdoc/>
        public IDisposable RegisterRpcHandler<TRpc, TRpcResult>(IAsyncMessageRpcHandler<TRpc, TRpcResult> handler)
            where TRpc : IMessageRpc<TRpcResult>
            where TRpcResult : IMessageRpcResult
        {
            ThrowDisposed();
            return InnerRegisterRpcHandler<TRpc, TRpcResult, IAsyncMessageRpcHandler<TRpc, TRpcResult>>(
                handler,
                m => handler.HandleAsync(m).ConfigureAwait(false).GetAwaiter().GetResult()
            );
        }

        private IDisposable InnerRegisterEventHandler<TEvent, TEventHandler>(TEventHandler handler, Func<IMessage<TEvent>, Task> execute)
            where TEvent : IMessageEvent
        {
            IDisposable subscription = _broker
                .Events(GetTopicNameFromType(typeof(TEvent)))
                .Subscribe<TEvent>(message =>
                {
                    ExecuteMessage(message, m => WithExceptionNotification(m.Payload.MessageId, m.Payload, () => execute(m)));
                    message.Ack();
                });

            return AssignSubscription(handler, subscription);
        }

        private IDisposable InnerRegisterCommandHandler<TCommand, TCommandHandler>(TCommandHandler handler, Func<TCommand, Task> execute)
            where TCommand : IMessageCommand
        {
            IDisposable subscription = _broker
                .Commands(GetTopicNameFromType(typeof(TCommand)))
                .Subscribe<TCommand>(message =>
                {
                    ExecuteMessage(message, m => HandleCommandMessage(m, p => execute(p)));
                });

            return AssignSubscription(handler, subscription);
        }

        private IDisposable InnerRegisterQueryHandler<TQuery, TQueryResult, TQueryHandler>(TQueryHandler handler, Func<TQuery, TQueryResult> execute)
            where TQuery : IMessageQuery<TQueryResult>
            where TQueryResult : IMessageQueryResult
        {
            IDisposable subscription = _broker
                .Commands(GetTopicNameFromType(typeof(TQuery)))
                .Subscribe<TQuery>(message =>
                {
                    ExecuteMessage(message, m => HandleQueryMessage(m, (p) => execute(p)));
                });

            return AssignSubscription(handler, subscription);
        }

        private IDisposable InnerRegisterRpcHandler<TRpc, TRpcResult, TRpcHandler>(TRpcHandler handler, Func<TRpc, TRpcResult> execute)
            where TRpc : IMessageRpc<TRpcResult>
            where TRpcResult : IMessageRpcResult
        {
            IDisposable subscription = _broker
                .Commands(GetTopicNameFromType(typeof(TRpc)))
                .Subscribe<TRpc>(message =>
                {
                    ExecuteMessage(message, m => HandleRpcMessage(m, (p) => execute(p)));
                });

            return AssignSubscription(handler, subscription);
        }

        private static IDisposable AssignSubscription<T>(T handler, IDisposable subscription)
        {
            if (handler is ISubscriptionAwareHandler subscriptionAware)
                subscriptionAware.RegisterSubscription(subscription);
            return subscription;
        }

        protected virtual void ExecuteMessage<TEvent>(IMessage<TEvent> message, Func<IMessage<TEvent>, Task> handler)
        {
            handler(message).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        private void NotifyException(MessageId messageId, object message, Exception exception)
        {
            _exceptionNotification.NotifyException(messageId, message, exception);
        }

        private async Task WithExceptionNotification(MessageId messageId, object message, Func<Task> execution)
        {
            try
            {
                await execution();
            }
            catch (Exception ex)
            {
                NotifyException(messageId, message, ex);
                throw;
            }
        }

        private Task HandleCommandMessage<TCommand>(IMessage<TCommand> message, Func<TCommand, Task> run)
            where TCommand : IMessageCommand
        {
            return HandleMessageAndPublishOutcome(
                GetOutcomeTopicName(message.Payload),
                message,
                command =>
                {
                    ExecuteMessage(message, m => WithExceptionNotification(m.Payload.MessageId, m.Payload, () => run(m.Payload)));
                    return MessageCommandOutcome.Success(message.Payload.MessageId);
                },
                CreateCommandFailureResult
            );
        }

        private Task HandleQueryMessage<TQuery, TQueryResult>(IMessage<TQuery> message, Func<TQuery, TQueryResult> run)
            where TQuery : IMessageQuery<TQueryResult>
            where TQueryResult : IMessageQueryResult
        {
            return HandleMessageAndPublishOutcome<TQuery, IMessageQueryResult>(
                GetOutcomeTopicName(message.Payload),
                message,
                m => run(m),
                CreateQueryFailureResult
            );
        }

        private Task HandleRpcMessage<TRpc, TRpcResult>(IMessage<TRpc> message, Func<TRpc, TRpcResult> run)
            where TRpc : IMessageRpc<TRpcResult>
            where TRpcResult : IMessageRpcResult
        {
            return HandleMessageAndPublishOutcome<TRpc, IMessageRpcResult>(
                GetOutcomeTopicName(message.Payload),
                message,
                m => run(m),
                CreateRpcFailureResult
            );
        }

        private Task HandleMessageAndPublishOutcome<TIncomming, TOutcome>(
            TopicName outcomeTopic,
            IMessage<TIncomming> message,
            Func<TIncomming, TOutcome> run,
            Func<IMessage<TIncomming>, Exception, TOutcome> onException)
            where TIncomming : IHasMessageId
        {
            TOutcome outcome;
            try
            {
                outcome = run(message.Payload);
                message.Ack();
            }
            catch (Exception ex)
            {
                NotifyException(message.Payload.MessageId, message.Payload, ex);
                outcome = onException(message, ex);
                message.Nack();
            }
            return _broker.Publish(outcome, outcomeTopic);
        }

        private static IMessageQueryResult.FailureResult CreateQueryFailureResult<TQuery>(IMessage<TQuery> message, Exception ex)
            where TQuery : IHasMessageId
        {
            return new IMessageQueryResult.FailureResult(message.Payload.MessageId, ex);
        }

        private static IMessageRpcResult.FailureResult CreateRpcFailureResult<TRpc>(IMessage<TRpc> message, Exception ex)
            where TRpc : IHasMessageId
        {
            return new IMessageRpcResult.FailureResult(message.Payload.MessageId, ex);
        }

        private static MessageCommandOutcome CreateCommandFailureResult<TCommand>(IMessage<TCommand> message, Exception ex)
            where TCommand : IHasMessageId
        {
            return MessageCommandOutcome.Failure(message.Payload.MessageId, ex);
        }
    }
}
