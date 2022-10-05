using System;

namespace MessageBus.Messaging.InProcess
{
    /// <summary>
    /// Executes all provided actions within the caller thread.
    /// </summary>
    /// <remarks>Do not use this class in production - it can result in a dead lock.</remarks>
    public sealed class BlockingEventExecuter : IEventExecuter
    {
        public Action Wrap(Action action)
        {
            return action;
        }
    }
}
