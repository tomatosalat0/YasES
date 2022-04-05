using System;
using System.Collections.Generic;
using System.Linq;

namespace YasES.Core
{
    /// <summary>
    /// All collected events will get the defined correlation id set in their
    /// header. If a passed event already contains a correlation id, an exception
    /// is thrown if the correlation id are not equal.
    /// </summary>
    public class CorrelationEventCollector : EventHeaderDecorator, IEventCollector
    {
        private readonly IEventCollector _inner;

        public CorrelationEventCollector(string correlationId, IEventCollector inner)
            : base(CommonMetaData.CorrelationId, () => correlationId)
        {
            if (correlationId is null) throw new ArgumentNullException(nameof(correlationId));
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

    public static class CorrelationEventCollectorExtensions
    {
        public static IEventCollector AssignCorrelationId(this IEventCollector collector, string correlationId)
        {
            return new CorrelationEventCollector(correlationId, collector);
        }
    }
}
