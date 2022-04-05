using System;

namespace YasES.Core
{
    public class EventStoreBuilder
    {
        private readonly Container _container = new Container();

        public static EventStoreBuilder Init()
        {
            return new EventStoreBuilder();
        }

        public virtual EventStoreBuilder ConfigureContainer(Action<Container> handler)
        {
            handler(_container);
            return this;
        }

        public virtual IEventStore Build()
        {
            return new DefaultEventStore(_container);
        }
    }
}
