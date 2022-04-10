using YasES.Persistance.InMemory;

namespace YasES.Core
{
    public static class EventStoreBuilderMemoryPersistanceExtensions
    {
        public static EventStoreBuilder UseInMemoryPersistance(this EventStoreBuilder wireup)
        {
            return wireup.ConfigureServices((container) =>
            {
                container.RegisterSingleton<IEventReadWrite>((_) => new InMemoryPersistanceEngine());
            });
        }
    }
}
