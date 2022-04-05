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
            if (!attempt.StreamIdentifier.IsSingleStream)
                throw new InvalidOperationException($"The stream identifier must point to a single stream");

            MessageListContainer container = OpenContainer(attempt.StreamIdentifier)[0];
            lock(_commitLock)
            {
                if (!_commitedIds.Add(attempt.CommitId))
                    return;

                _nextSequenceId = container.Append(attempt, _nextSequenceId);
            }
        }

        public IEnumerable<IStoredEventMessage> Read(ReadPredicate predicate)
        {
            MessageListIterator? iterator = PrepareIterator(predicate.Streams);
            if (iterator == null)
                return Enumerable.Empty<IStoredEventMessage>();

            IEnumerable<IStoredEventMessage> messages = predicate.Reverse
                ? iterator.Backward(predicate.LowerExclusiveBound, predicate.UpperExclusiveBound)
                : iterator.Forward(predicate.LowerExclusiveBound, predicate.UpperExclusiveBound);

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
                return identifiers.Distinct().SelectMany(OpenContainerUnlocked).Distinct().ToList();
            }
        }

        private List<MessageListContainer> OpenContainer(StreamIdentifier identifier)
        {
            lock(_streamsLock)
            {
                return OpenContainerUnlocked(identifier);
            }
        }

        private List<MessageListContainer> OpenContainerUnlocked(StreamIdentifier identifier)
        {
            List<MessageListContainer> result = new List<MessageListContainer>();
            if (identifier.IsSingleStream)
            {
                if (!_streams.TryGetValue(identifier, out MessageListContainer? container))
                {
                    container = new MessageListContainer(identifier);
                    _streams.Add(identifier, container);
                }
                result.Add(container);
            } else
            {
                result.AddRange(_streams
                    .Where(p => p.Key.BucketId == identifier.BucketId)
                    .Where(p => p.Key.StreamId.StartsWith(identifier.StreamIdPrefix, StringComparison.Ordinal))
                    .Select(p => p.Value)
                );
            }
            return result;
        }
    }
}
