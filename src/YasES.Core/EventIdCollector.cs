using System;
using System.Collections.Generic;
using System.Linq;

namespace YasES.Core
{
    /// <summary>
    /// All collected events will get a new event id.
    /// </summary>
    public class EventIdCollector : EventHeaderDecorator, IEventCollector
    {
        private readonly IEventCollector _inner;

        public EventIdCollector(IEventCollector inner)
            : base(CommonMetaData.EventId, () => Guid.NewGuid().ToString())
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public bool IsEmpty => _inner.IsEmpty;

        public bool IsCommited => _inner.IsCommited;

        public IEventCollector Add(IEnumerable<IEventMessage> messages)
        {
            _inner.Add(messages.Select(Decorate));
            return this;
        }

        public CommitAttempt BuildCommit(StreamIdentifier stream, CommitId commitId)
        {
            return _inner.BuildCommit(stream, commitId);
        }
    }

    public static class EventIdCollectorExtensions
    {
        public static IEventCollector AutoCreateEventId(this IEventCollector collector)
        {
            return new EventIdCollector(collector);
        }
    }
}
