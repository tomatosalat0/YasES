using System;

namespace YasES.Core
{
    public class DefaultEventStore : IEventStore
    {
        private readonly Container _container;
        private bool _disposedValue;

        public DefaultEventStore(Container container)
        {
            _container = container;
            Services = container;
            Events = container.Resolve<IEventReadWrite>();
        }

        public IEventReadWrite Events { get; }

        public Container Services { get; } 

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                _disposedValue = true;
                if (disposing)
                {
                    Services.Dispose();
                    _container.Dispose();
                }
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
