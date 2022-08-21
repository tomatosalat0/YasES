using System;
using System.Threading;
using System.Threading.Tasks;

namespace MessageBus
{
    public interface IMessageBusAwait : IDisposable
    {
        /// <summary>
        /// Creates an awaitable handle for the specified <typeparamref name="TEvent"/>.
        /// To await for the event, call <see cref="AwaitHandle{TEvent}.Await"/>. It will
        /// return as soon as an event which matches the provided <paramref name="predicate"/>
        /// was received.
        /// </summary>
        AwaitHandle<TEvent> AwaitEvent<TEvent>(Func<TEvent, bool> predicate, CancellationToken cancellationToken)
            where TEvent : IMessageEvent;
    }

    public static class IMessageBusAwaitExtensions
    {
        public static AwaitHandle<TEvent> AwaitEvent<TEvent>(this IMessageBusAwait messageBusAwait, CancellationToken cancellationToken)
            where TEvent : IMessageEvent
        {
            return messageBusAwait.AwaitEvent<TEvent>(_ => true, cancellationToken);
        }
    }

    /// <summary>
    /// Allows to wait until the previous defined condition are met.
    /// This class auto disposes itself within the <see cref="Await"/>. So you don't need
    /// to call Dispose by yourself.
    /// </summary>
    public sealed class AwaitHandle<TEvent> : IDisposable
        where TEvent : IMessageEvent
    {
        private readonly Task<TEvent> _handle;
        private readonly IDisposable _subscription;
        private bool _disposedValue;

        public AwaitHandle(Task<TEvent> handle, IDisposable subscription)
        {
            _handle = handle;
            _subscription = subscription;
        }

        public async Task<TEvent> Await()
        {
            try
            {
                return await _handle;
            }
            finally
            {
                Dispose();
            }
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _subscription.Dispose();
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
}
