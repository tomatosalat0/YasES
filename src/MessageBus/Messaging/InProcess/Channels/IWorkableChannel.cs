using System;
using System.Collections.Generic;

namespace MessageBus.Messaging.InProcess.Channels
{
    internal interface IWorkableChannel : IChannel
    {
        void Cleanup();

        IReadOnlyList<Action> CollectWork();
    }
}
