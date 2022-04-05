using System;
using System.Data;
using System.Diagnostics;
using YasES.Core;

namespace YasES.Persistance.Sqlite
{
    internal class LoggingPageReader : IPageReader
    {
        private readonly IDbCommand _command;
        private readonly IPageReader _inner;
        private readonly Stopwatch _stopwatch;
        private int _returnedItems = 0;
        private bool _disposedValue;

        public LoggingPageReader(IPageReader inner, IDbCommand command)
        {
            _inner = inner;
            _command = command;
            _stopwatch = new Stopwatch();
        }

        public void Start()
        {
            _stopwatch.Start();
            _inner.Start();
            _stopwatch.Stop();
        }

        public bool TryReadNext(out IStoredEventMessage message)
        {
            _stopwatch.Start();
            bool result = _inner.TryReadNext(out message);
            _stopwatch.Stop();
            if (result)
                _returnedItems++;
            return result;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _inner.Dispose();
                    Debugger.Log(0, null, $"Duration {_stopwatch.Elapsed}, Returned: {_returnedItems}\nCommand: {_command.CommandText}\n");
                }
                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
