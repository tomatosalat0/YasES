using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using MessageBus.Messaging.InProcess.Messages;

namespace MessageBus.Messaging.InProcess.Channels
{
    internal class CommandsChannel : IWorkableChannel
    {
        private readonly ConcurrentQueue<Message> _pendingWork = new ConcurrentQueue<Message>();
        private readonly ConcurrentQueue<ChannelSubscription> _availableSubscriptions = new ConcurrentQueue<ChannelSubscription>();
        private readonly Action _awake;

        public CommandsChannel(Action awake)
        {
            _awake = awake ?? throw new ArgumentNullException(nameof(awake));
        }

        public void Cleanup()
        {
            int expectedIterations = _availableSubscriptions.Count;
            for (int i = 0; i < expectedIterations; i++)
            {
                if (!_availableSubscriptions.TryDequeue(out var subscription))
                    return;

                if (subscription.IsActive)
                    _availableSubscriptions.Enqueue(subscription);
            }
        }

        public IReadOnlyList<Action> CollectWork()
        {
            List<Action> result = new List<Action>();
            while (TryCollectAction(out Action? execute))
                result.Add(execute);
            return result;
        }

        private bool TryCollectAction([NotNullWhen(true)] out Action? action)
        {
            if (!_pendingWork.TryPeek(out _))
                goto fail;

            ChannelSubscription? subscription;
            do
            {
                if (!_availableSubscriptions.TryDequeue(out subscription))
                    goto fail;
            } while (!subscription.IsActive);

            if (!_pendingWork.TryDequeue(out Message? work))
            {
                _availableSubscriptions.Enqueue(subscription);
                goto fail;
            }

            subscription.Enqueue(work);
            action = BuildWorkAction(subscription);
            return true;
        fail:
            action = null;
            return false;
        }

        private Message? TryFetchAdHoc()
        {
            if (!_pendingWork.TryDequeue(out Message? result))
                return null;
            return result;
        }

        private static Action BuildWorkAction(ChannelSubscription subscription)
        {
            return subscription.DoWorkAction;
        }

        private void HandleWorkComplete(ChannelSubscription subscription)
        {
            _availableSubscriptions.Enqueue(subscription);
            if (!_pendingWork.IsEmpty)
                _awake();
        }

        public IDisposable Subscribe<T>(Action<IMessage<T>> messageHandler) where T : notnull
        {
            if (messageHandler is null) throw new ArgumentNullException(nameof(messageHandler));
            ChannelSubscription result = new ChannelSubscription(WrapExceptionHandler(a => messageHandler((Message<T>)a)), HandleWorkComplete, TryFetchAdHoc);
            _availableSubscriptions.Enqueue(result);
            return result;
        }

        public IDisposable Subscribe(Action<IMessage> messageHandler)
        {
            if (messageHandler is null) throw new ArgumentNullException(nameof(messageHandler));
            ChannelSubscription result = new ChannelSubscription(WrapExceptionHandler(a => messageHandler(a)), HandleWorkComplete, TryFetchAdHoc);
            _availableSubscriptions.Enqueue(result);
            return result;
        }

        private Action<Message> WrapExceptionHandler(Action<Message> handler)
        {
            return (m) =>
            {
                try
                {
                    handler(m);
                    HandleAfterHandle(m);
                }
                catch (Exception ex)
                {
                    HandleException(m, ex);
                }
            };
        }

        private void HandleAfterHandle(Message message)
        {
            if (message.State == MessageState.Initial)
            {
                _pendingWork.Enqueue(new Message(message.Payload));
                _awake();
            }
        }

        private void HandleException(Message message, Exception exception)
        {
            if (message.State != MessageState.Acknowledged)
            {
                _pendingWork.Enqueue(new Message(message.Payload));
                _awake();
            }
        }

        public Task Publish<T>(T message)
        {
            if (message is null) throw new ArgumentNullException(nameof(message));
            _pendingWork.Enqueue(new Message(message));
            _awake();
            return Task.CompletedTask;
        }
    }
}
