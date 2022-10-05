using System;
using System.Threading;

namespace MessageBus.Messaging.InProcess.Execution
{
    internal interface ICollectable
    {
        bool IsCompleted { get; }

        bool HasCollectables { get; }

        bool WaitFor(TimeSpan timeout, CancellationToken cancellationToken);

        void Collect();
    }
}
