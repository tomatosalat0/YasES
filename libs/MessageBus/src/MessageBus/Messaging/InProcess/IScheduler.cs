using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace MessageBus.Messaging.InProcess
{
    public interface IScheduler : IDisposable
    {
    }

    public interface IWorkFactory
    {
        /// <summary>
        /// Returns true if there is work available, otherwise false. Note that this
        /// method won't wait for work to be available.
        /// </summary>
        bool HasWork();

        /// <summary>
        /// Waits for the next work item to be available. Returns true if work is available, otherwise false.
        /// Returns false if the specified <paramref name="timeout"/> has been reached.
        /// If the result is true, a <paramref name="workToExecute"/> item gets returned.
        /// The returned item must get executed by calling <see cref="IWork.Execute"/>.
        /// </summary>
        /// <exception cref="OperationCanceledException">Is thrown if <paramref name="cancellationToken"/> is
        /// cancelled.</exception>
        bool TryWaitForWork(TimeSpan timeout, CancellationToken cancellationToken, [NotNullWhen(true)] out IWork? workToExecute);

        public interface IWork
        {
            /// <summary>
            /// Do the work.
            /// </summary>
            void Execute();
        }
    }
}
