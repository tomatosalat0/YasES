using YasES.Persistance.InMemory;

namespace YasES.Core
{
    public static class EventStoreBuilderMemoryPersistanceExtensions
    {
        public static EventStoreBuilder UseInMemoryPersistance(this EventStoreBuilder wireup)
        {
            return wireup.ConfigureContainer((container) => 
            {
                container.Register<IEventReadWrite>((_) => new InMemoryPersistanceEngine());
            });
        }
    }
}
