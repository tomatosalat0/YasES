using System;

namespace MessageBus.Messaging.InProcess
{
    public interface IEventExecuter
    {
        /// <summary>
        /// Wraps the provided <paramref name="action"/>. The returned action
        /// will get called to perform the <paramref name="action"/>.
        /// </summary>
        Action Wrap(Action action);
    }
}
