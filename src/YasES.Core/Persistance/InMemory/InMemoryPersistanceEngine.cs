using System;
using System.Collections.Generic;
using System.Linq;
using YasES.Core;

namespace YasES.Persistance.InMemory
{
    public class InMemoryPersistanceEngine : IEventReadWrite, IEventRead, IEventWrite
    {
        private readonly Dictionary<StreamIdentifier, MessageListContainer> _streams = new Dictionary<StreamIdentifier, MessageListContainer>();
        private readonly HashSet<CommitId> _commitedIds = new HashSet<CommitId>();
        private readonly object _streamsLock = new object();
        private long _nextSequenceId = 1;
        private readonly object _commitLock = new object();

        public void Commit(CommitAttempt attempt)
        {
            MessageListContainer container = OpenContainer(attempt.StreamIdentifier);
            lock(_commitLock)
            {
                if (!_commitedIds.Add(attempt.CommitId))
                    return;

                _nextSequenceId = container.Append(attempt, _nextSequenceId);
            }
        }

        public IEnumerable<IReadEventMessage> Read(ReadPredicate predicate)
        {
            MessageListIterator? iterator = PrepareIterator(predicate.Streams);
            if (iterator == null)
                return Enumerable.Empty<IReadEventMessage>();

            IEnumerable<IReadEventMessage> messages = predicate.Reverse
                ? iterator.Backward(predicate.LowerBound, predicate.UpperBound)
                : iterator.Forward(predicate.LowerBound, predicate.UpperBound);

            if (predicate.EventNamesFilter != null)
            {
                messages = predicate.EventNamesIncluding
                    ? messages.Where(p => predicate.EventNamesFilter.Contains(p.EventName))
                    : messages.Where(p => !predicate.EventNamesFilter.Contains(p.EventName));
            }
            if (predicate.CorrelationId != null)
            {
                messages = messages.Where(p => predicate.CorrelationId == p.Headers.GetValueOrDefault(CommonMetaData.CorrelationId) as string);
            }

            return messages;
        }

        private MessageListIterator? PrepareIterator(IReadOnlyList<StreamIdentifier> streams)
        {
            IReadOnlyList<MessageListContainer> sources = OpenSelectedContainer(streams);
            if (sources.Count == 0)
                return null;
            MessageListIterator iterator = new MessageListIterator(sources);
            return iterator;
        }

        private IReadOnlyList<MessageListContainer> OpenSelectedContainer(IReadOnlyList<StreamIdentifier> identifiers)
        {
            if (identifiers.Count == 0)
                return Array.Empty<MessageListContainer>();

            if (identifiers.Any(p => p.MatchesAllStreams))
            {
                lock (_streamsLock)
                    return _streams.Values.ToList();
            }

            lock(_streamsLock)
            {
                return identifiers.Distinct().Select(OpenContainerUnlocked).ToList();
            }
        }

        private MessageListContainer OpenContainer(StreamIdentifier identifier)
        {
            lock(_streamsLock)
            {
                return OpenContainerUnlocked(identifier);
            }
        }

        private MessageListContainer OpenContainerUnlocked(StreamIdentifier identifier)
        {
            if (!_streams.TryGetValue(identifier, out MessageListContainer? container))
            {
                container = new MessageListContainer(identifier);
                _streams.Add(identifier, container);
            }
            return container;
        }
    }
}
