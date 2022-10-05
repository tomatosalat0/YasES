using System;

namespace MessageBus.Decorators
{
    public abstract class DecoratedSubscriptionAwareHandler : ISubscriptionAwareHandler
    {
        private readonly object _inner;

        protected DecoratedSubscriptionAwareHandler(object inner)
        {
            _inner = inner;
        }

        public void RegisterSubscription(IDisposable subscription)
        {
            (_inner as ISubscriptionAwareHandler)?.RegisterSubscription(subscription);
        }
    }
}
