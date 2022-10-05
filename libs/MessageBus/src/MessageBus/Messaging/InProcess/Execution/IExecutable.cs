using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace MessageBus.Messaging.InProcess.Execution
{
    internal interface IExecutable
    {
        bool IsCompleted { get; }

        bool HasExecutables { get; }

        bool TryTake([NotNullWhen(true)] out Action? action, TimeSpan timeout, CancellationToken cancellationToken);
    }
}
