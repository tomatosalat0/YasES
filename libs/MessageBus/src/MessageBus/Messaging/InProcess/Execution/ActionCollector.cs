using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace MessageBus.Messaging.InProcess.Execution
{
    internal class ActionCollector : IWorkFactory
    {
        private readonly ICollectable _source;

        public ActionCollector(ICollectable source)
        {
            _source = source;
        }

        public bool HasWork()
        {
            return _source.HasCollectables;
        }

        public bool TryWaitForWork(TimeSpan timeout, CancellationToken cancellationToken, [NotNullWhen(true)] out IWorkFactory.IWork? workToExecute)
        {
            if (_source.IsCompleted)
                goto fail;

            if (!_source.WaitFor(timeout, cancellationToken))
                goto fail;

            workToExecute = new Work(_source);
            return true;

        fail:
            workToExecute = null;
            return false;
        }

        private readonly struct Work : IWorkFactory.IWork
        {
            private readonly ICollectable _source;

            public Work(ICollectable source)
            {
                _source = source;
            }

            public void Execute()
            {
                _source.Collect();
            }
        }
    }
}
