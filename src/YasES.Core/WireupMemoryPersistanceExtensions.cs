using YasES.Persistance.InMemory;

namespace YasES.Core
{
    public static class WireupMemoryPersistanceExtensions
    {
        public static Wireup UseInMemoryPersistance(this Wireup wireup)
        {
            return wireup.ConfigureContainer((container) => 
            {
                container.Register<IEventReadWrite>((_) => new InMemoryPersistanceEngine());
            });
        }
    }
}
