using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace YasES.Plugins.Messaging
{
    /// <summary>
    /// This scheduler will call the <see cref="IBrokerCommands"/> from different in the background. 
    /// This way all events will get forwarded to each subscriber automatically.
    /// </summary>
    public class ThreadedBrokerScheduling : IDisposable
    {
        private readonly IBrokerCommands _scheduling;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly IReadOnlyList<Thread> _threads;
        private bool _disposedValue;

        public ThreadedBrokerScheduling(int numberOfWorkerThreads, IBrokerCommands scheduling)
        {
            _scheduling = scheduling ?? throw new ArgumentNullException(nameof(scheduling));
            _threads = BuildWorkerThreads(numberOfWorkerThreads);
        }

        public int NumberOfThreads => _threads.Count;

        private IReadOnlyList<Thread> BuildWorkerThreads(int numberOfWorkerThreads)
        {
            if (numberOfWorkerThreads < 1)
                throw new ArgumentOutOfRangeException(nameof(numberOfWorkerThreads), $"The number of worker threads must not be smaller than 1, got {numberOfWorkerThreads}");

            List<Thread> threads = new List<Thread>();
            foreach (var index in Enumerable.Range(0, numberOfWorkerThreads))
            {
                Thread workerThread = new Thread(WorkerThreadMain);
                workerThread.Name = $"{nameof(MessageBroker)}.BackgroundWorker.{index}";
                threads.Add(workerThread);
                workerThread.Start();
            }
            return threads;
        }

        private void WorkerThreadMain(object? state)
        {
            try
            {
                WorkerThreadMainInner();
            }
            catch (Exception)
            {
            }
        }

        private void WorkerThreadMainInner()
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                if (_scheduling.WaitForMessages(millisecondsTimeout: 5000, _cancellationTokenSource.Token))
                    WorkerThreadWakeup();
                else
                    WorkerThreadCleanup();
            }
        }

        private void WorkerThreadCleanup()
        {
            _scheduling.RemoveEmptyChannels();
        }

        private void WorkerThreadWakeup()
        {
            while (_scheduling.CallSubscribers() > 0)
            {
                _cancellationTokenSource.Token.ThrowIfCancellationRequested();
            }
        }

        private void StopAndWait()
        {
            _cancellationTokenSource.Cancel();
            foreach (var thread in _threads)
                thread.Join();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    StopAndWait();
                    _cancellationTokenSource.Dispose();
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
