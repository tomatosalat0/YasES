using System;
using System.Collections.Generic;
using System.Threading;

namespace MessageBus.Messaging.InProcess
{
    internal class LockedDictionary<TKey, TValue> : IDisposable
        where TKey : notnull
    {
        private readonly Dictionary<TKey, TValue> _value = new Dictionary<TKey, TValue>();
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        private bool _disposedValue;

        public int Count => Read(c => c.Count);

        public TResult Read<TResult>(Func<IReadOnlyDictionary<TKey, TValue>, TResult> handle)
        {
            if (handle is null) throw new ArgumentNullException(nameof(handle));
            _lock.EnterReadLock();
            try
            {
                return handle(_value);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public void Write(Action<IDictionary<TKey, TValue>> handle)
        {
            if (handle is null) throw new ArgumentNullException(nameof(handle));
            _lock.EnterWriteLock();
            try
            {
                handle(_value);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public TResult Write<TResult>(Func<IDictionary<TKey, TValue>, TResult> handle)
        {
            if (handle is null) throw new ArgumentNullException(nameof(handle));
            _lock.EnterWriteLock();
            try
            {
                return handle(_value);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _lock.Dispose();
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
