using System;
using System.Collections.Generic;
using System.Linq;
using YasES.Core;

namespace YasES.Persistance.InMemory
{
    internal class MessageListIterator 
    {
        private readonly IReadOnlyList<MessageListContainer> _container;

        public MessageListIterator(IReadOnlyList<MessageListContainer> container)
        {
            _container = container;
        }

        internal IEnumerable<IStoredEventMessage> Forward(CheckpointToken lowerBoundExclusive, CheckpointToken upperBoundExclusive)
        {
            List<IStoredEventMessage> result = BuildSnapshot(p => p.Checkpoint > lowerBoundExclusive && p.Checkpoint < upperBoundExclusive)
                .OrderBy(p => p.Checkpoint)
                .ToList();
            return result;
        }

        internal IEnumerable<IStoredEventMessage> Backward(CheckpointToken lowerBoundExclusive, CheckpointToken upperBoundExclusive)
        {
            List<IStoredEventMessage> result = BuildSnapshot(p => p.Checkpoint > lowerBoundExclusive && p.Checkpoint < upperBoundExclusive)
                .OrderByDescending(p => p.Checkpoint)
                .ToList();
            return result;
        }

        private IReadOnlyList<IStoredEventMessage> BuildSnapshot(Func<IStoredEventMessage, bool> predicate)
        {
            List<IStoredEventMessage> result = new List<IStoredEventMessage>(
                _container.SelectMany(p => p.CreateSnapshot(predicate))
            );
            return result;
        }
    }
}
