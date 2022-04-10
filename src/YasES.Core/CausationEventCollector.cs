using System;
using System.Collections.Generic;
using System.Linq;

namespace YasES.Core
{
    /// <summary>
    /// All collected events will get the defined causation id set in their
    /// header. If a causation id is already set, it won't get adjusted.
    /// </summary>
    public class CausationEventCollector : EventHeaderDecorator, IEventCollector
    {
        private readonly IEventCollector _inner;

        public CausationEventCollector(string causationId, IEventCollector inner)
            : base(CommonMetaData.CausationId, () => causationId)
        {
            if (causationId is null) throw new ArgumentNullException(nameof(causationId));
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

    public static class CausationEventCollectorExtensions
    {
        public static IEventCollector AssignCausationId(this IEventCollector collector, string causationId)
        {
            return new CausationEventCollector(causationId, collector);
        }
    }
}
