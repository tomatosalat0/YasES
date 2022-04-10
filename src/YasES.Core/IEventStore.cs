using System;

namespace YasES.Core
{
    public interface IEventStore : IDisposable
    {
        IEventReadWrite Events { get; }

        ServiceCollection Services { get; }
    }
}
