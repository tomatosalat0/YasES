using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace MessageBus.Messaging.InProcess.Scheduler
{
    public sealed class ManualScheduler : ISchedulerFactory
    {
        private readonly List<Manual> _scheduler = new List<Manual>();

        public IScheduler Create(IWorkFactory workType)
        {
            Manual result = new Manual(workType);
            _scheduler.Add(result);
            return result;
        }

        private sealed class Manual : IScheduler
        {
            private readonly IWorkFactory _factory;

            public Manual(IWorkFactory factory)
            {
                _factory = factory;
            }

            internal bool HasWork()
            {
                return _factory.HasWork();
            }

            public bool ExecuteOnce()
            {
                if (!HasWork())
                    return false;

                if (!_factory.TryWaitForWork(TimeSpan.FromMilliseconds(100), CancellationToken.None, out var work))
                    return false;

                work.Execute();
                return true;
            }

            public int Drain()
            {
                int count = 0;
                while (ExecuteOnce())
                {
                    count++;
                }
                return count;
            }

            public void Dispose()
            {
                GC.SuppressFinalize(this);
            }
        }

        public bool HasWork()
        {
            return _scheduler.Any(p => p.HasWork());
        }

        public void ExecuteOnce()
        {
            foreach (var p in _scheduler)
                p.ExecuteOnce();
        }

        public void Drain()
        {
            bool anyOneHasMessage;
            do
            {
                anyOneHasMessage = false;
                foreach (var p in _scheduler)
                    anyOneHasMessage = (p.Drain() > 0) || anyOneHasMessage;
            } while (anyOneHasMessage);
        }
    }
}
