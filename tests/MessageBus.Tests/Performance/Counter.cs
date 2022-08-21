using System;
using System.Threading;

namespace MessageBus.Tests.Performance
{
    internal class Counter : IDisposable
    {
        private readonly ManualResetEventSlim _event = new ManualResetEventSlim();
        private readonly long _expectedCount;
        private long _value;
        private bool disposedValue;

        public Counter(long expectedCount)
        {
            _expectedCount = expectedCount;
        }

        public long Value => _value;

        public void Increment()
        {
            long newValue = Interlocked.Increment(ref _value);
            if (newValue == _expectedCount)
                _event.Set();
        }

        public bool Wait(TimeSpan timeout)
        {
            if (_value == _expectedCount)
                return true;
            return _event.Wait(timeout);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _event.Dispose();
                }
                disposedValue = true;
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
