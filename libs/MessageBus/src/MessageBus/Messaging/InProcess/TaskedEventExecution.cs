using System;
using System.Threading.Tasks;

namespace MessageBus.Messaging.InProcess
{
    /// <summary>
    /// Executes all given actions within its own <see cref="Task"/>.
    /// </summary>
    public sealed class TaskedEventExecution : IEventExecuter
    {
        public Action Wrap(Action action)
        {
            return () => Task.Run(action);
        }
    }
}
