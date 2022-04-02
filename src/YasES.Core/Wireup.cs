using System;

namespace YasES.Core
{
    public class Wireup
    {
        private readonly Container _container = new Container();

        public static Wireup Init()
        {
            return new Wireup();
        }

        public Wireup ConfigureContainer(Action<Container> handler)
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
