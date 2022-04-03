using System;

namespace YasES.Core
{
    public interface IEventStore : IDisposable
    {
        IEventReadWrite Events { get; }

        Container Services { get; }
    }
}
