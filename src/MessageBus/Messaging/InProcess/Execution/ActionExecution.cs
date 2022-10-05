using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace MessageBus.Messaging.InProcess.Execution
{
    internal class ActionExecution : IWorkFactory
    {
        private readonly IExecutable _source;

        public ActionExecution(IExecutable source)
        {
            _source = source;
        }

        public bool HasWork()
        {
            return _source.HasExecutables;
        }

        public bool TryWaitForWork(TimeSpan timeout, CancellationToken cancellationToken, [NotNullWhen(true)] out IWorkFactory.IWork? workToExecute)
        {
            if (_source.IsCompleted)
                goto fail;

            if (!_source.TryTake(out Action? action, timeout, cancellationToken))
                goto fail;

            workToExecute = new Work(action);
            return true;

        fail:
            workToExecute = null;
            return false;
        }

        private readonly struct Work : IWorkFactory.IWork
        {
            private readonly Action _action;

            public Work(Action action)
            {
                _action = action;
            }

            public void Execute()
            {
                _action();
            }
        }
    }
}
