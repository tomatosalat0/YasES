using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace MessageBus
{
    /// <summary>
    /// This part of the event bus is responsible for the <see cref="IMessageBusAwait"/>
    /// implementation.
    /// </summary>
    public partial class MessageBrokerMessageBus : IMessageBusAwait, IDisposable
    {
        /// <inheritdoc/>
        public AwaitHandle<TEvent> AwaitEvent<TEvent>(Func<TEvent, bool> predicate, CancellationToken cancellationToken) where TEvent : IMessageEvent
        {
            if (predicate is null) throw new ArgumentNullException(nameof(predicate));
            ThrowDisposed();

            var channel = Channel.CreateUnbounded<TEvent>(new UnboundedChannelOptions()
            {
                AllowSynchronousContinuations = true,
                SingleReader = true,
                SingleWriter = true
            });
            IDisposable subscription = MessageBusDelegateExtensions.RegisterEventDelegateAsync(this, CreateEventHandler(predicate, channel.Writer, cancellationToken));

            DisposeCallback onDispose = new DisposeCallback(subscription, cancellationToken);
            Task<TEvent> waitTask = CreateAwaitableTask(channel.Reader, onDispose.CancellationToken);

            return new AwaitHandle<TEvent>(waitTask, onDispose);
        }

        private sealed class DisposeCallback : IDisposable
        {
            private readonly IDisposable _inner;
            private readonly CancellationTokenSource _tokenSource;
            private bool _disposedValue;

            public DisposeCallback(IDisposable inner, CancellationToken token)
            {
                _tokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
                _inner = inner;
            }

            public CancellationToken CancellationToken => _tokenSource.Token;

            private void Dispose(bool disposing)
            {
                if (!_disposedValue)
                {
                    if (disposing)
                    {
                        _inner.Dispose();
                        _tokenSource.Cancel();
                        _tokenSource.Dispose();
                    }
                    _disposedValue = true;
                }
            }

            public void Dispose()
            {
                // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }
        }

        private static async Task<TEvent> CreateAwaitableTask<TEvent>(ChannelReader<TEvent> source, CancellationToken cancellationToken)
        {
            return await source.ReadAsync(cancellationToken);
        }

        private static Func<TEvent, Task> CreateEventHandler<TEvent>(Func<TEvent, bool> predicate, ChannelWriter<TEvent> target, CancellationToken cancellationToken)
        {
            return async (@event) =>
            {
                if (predicate(@event))
                    await target.WriteAsync(@event, cancellationToken);
            };
        }
    }
}
