using System;
using System.Diagnostics;

namespace MessageBus.Messaging.InProcess
{
    internal sealed class ChannelCleanupTime
    {
        private readonly TimeSpan _interval;
        private readonly Stopwatch _watch;

        public ChannelCleanupTime(TimeSpan interval)
        {
            _interval = interval;
            _watch = Stopwatch.StartNew();
        }

        public bool ShouldCleanup()
        {
            return _watch.Elapsed >= _interval;
        }

        public void Reset()
        {
            _watch.Restart();
        }
    }
}
