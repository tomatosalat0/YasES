using System;

namespace MessageBus
{
    public interface IMessageBus : IMessageBusHandler, IMessageBusPublishing, IMessageBusAwait, IDisposable
    {
    }
}
