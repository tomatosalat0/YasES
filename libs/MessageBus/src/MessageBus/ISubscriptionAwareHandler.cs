using System;

namespace MessageBus
{
    public interface ISubscriptionAwareHandler
    {
        void RegisterSubscription(IDisposable subscription);
    }
}
