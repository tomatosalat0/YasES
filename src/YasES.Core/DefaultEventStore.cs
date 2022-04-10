using System;

namespace YasES.Core
{
    public class DefaultEventStore : IEventStore
    {
        private readonly ServiceCollection _services;
        private bool _disposedValue;

        public DefaultEventStore(ServiceCollection services)
        {
            _services = services;
            Services = services;
            Events = services.Resolve<IEventReadWrite>();
        }

        public IEventReadWrite Events { get; }

        public ServiceCollection Services { get; }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                _disposedValue = true;
                if (disposing)
                {
                    Services.Dispose();
                    _services.Dispose();
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
