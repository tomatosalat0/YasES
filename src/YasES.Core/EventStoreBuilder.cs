using System;

namespace YasES.Core
{
#pragma warning disable CA1001 // Types that own disposable fields should be disposable (_services is passed to DefaultEventStore inside Build()
    public class EventStoreBuilder
#pragma warning restore CA1001 // Types that own disposable fields should be disposable
    {
        private readonly ServiceCollection _services = new ServiceCollection();
        private readonly EventStoreBuilder? _parent;

        public static EventStoreBuilder Init()
        {
            return new EventStoreBuilder();
        }

        internal EventStoreBuilder()
        {
        }

        public EventStoreBuilder(EventStoreBuilder parent)
        {
            _parent = parent;
        }

        public virtual EventStoreBuilder ConfigureServices(Action<ServiceCollection> handler)
        {
            if (_parent != null)
                _parent.ConfigureServices(handler);
            else
                handler(_services);
            return this;
        }

        public virtual IEventStore Build()
        {
            if (_parent != null)
                return _parent.Build();
            return new DefaultEventStore(_services);
        }
    }
}
