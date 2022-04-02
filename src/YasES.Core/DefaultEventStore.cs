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
            Events = container.Resolve<IEventReadWrite>();
        }

        public IEventReadWrite Events { get; }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _container.Dispose();
                }
                _disposedValue = true;
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
