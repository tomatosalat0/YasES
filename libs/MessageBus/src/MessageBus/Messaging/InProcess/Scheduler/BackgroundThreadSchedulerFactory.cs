using System;
using System.Threading;

namespace MessageBus.Messaging.InProcess.Scheduler
{
    public sealed class BackgroundThreadSchedulerFactory : ISchedulerFactory
    {
        private readonly string _threadName;

        public BackgroundThreadSchedulerFactory(string threadName)
        {
            if (string.IsNullOrWhiteSpace(threadName)) throw new ArgumentException($"'{nameof(threadName)}' cannot be null or whitespace.", nameof(threadName));
            _threadName = threadName;
        }

        public IScheduler Create(IWorkFactory workType)
        {
            return new BackgroundThreadScheduler(workType, _threadName);
        }

        internal sealed class BackgroundThreadScheduler : IScheduler
        {
            private readonly TimeSpan _timeout = TimeSpan.FromSeconds(2);
            private readonly IWorkFactory _workFactory;
            private readonly Thread _thread;
            private bool _disposedValue;

            public BackgroundThreadScheduler(IWorkFactory workFactory, string threadName)
            {
                _workFactory = workFactory;
                _thread = new Thread(BackgroundThreadMain);
                _thread.Name = threadName;
                _thread.IsBackground = true;
                _thread.Start();
            }

            private void BackgroundThreadMain(object? obj)
            {
                try
                {
                    BackgroundThread();
                }
                catch (Exception)
                {
                    // catch all to prevent runtime shutdown.
                }
            }

            private void BackgroundThread()
            {
                while (!_disposedValue)
                {
                    while (true)
                    {
                        if (_workFactory.TryWaitForWork(_timeout, CancellationToken.None, out var work))
                            work.Execute();
                    }
                }
            }

            private void Dispose(bool disposing)
            {
                if (!_disposedValue)
                {
                    if (disposing)
                    {
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
}
