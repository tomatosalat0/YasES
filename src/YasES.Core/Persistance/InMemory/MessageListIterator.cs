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

        internal IEnumerable<IReadEventMessage> Forward(CheckpointToken lowerBoundExclusive, CheckpointToken upperBoundExclusive)
        {
            List<IReadEventMessage> result = BuildSnapshot(p => p.Checkpoint > lowerBoundExclusive && p.Checkpoint < upperBoundExclusive)
                .OrderBy(p => p.Checkpoint)
                .ToList();
            return result;
        }

        internal IEnumerable<IReadEventMessage> Backward(CheckpointToken lowerBoundExclusive, CheckpointToken upperBoundExclusive)
        {
            List<IReadEventMessage> result = BuildSnapshot(p => p.Checkpoint > lowerBoundExclusive && p.Checkpoint < upperBoundExclusive)
                .OrderByDescending(p => p.Checkpoint)
                .ToList();
            return result;
        }

        private IReadOnlyList<IReadEventMessage> BuildSnapshot(Func<IReadEventMessage, bool> predicate)
        {
            List<IReadEventMessage> result = new List<IReadEventMessage>(
                _container.SelectMany(p => p.CreateSnapshot(predicate))
            );
            return result;
        }
    }
}
