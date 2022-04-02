using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace YasES.Core
{
    public class ReadPredicate
    {
        [ExcludeFromCodeCoverage]
        private ReadPredicate()
        {
            Streams = null!;
        }

        internal ReadPredicate(
            IReadOnlyList<StreamIdentifier> streams,
            bool reverse, 
            IReadOnlySet<string>? eventNamesFilter, 
            bool eventNamesIncluding, 
            string? correlationId,
            CheckpointToken lowerBound, 
            CheckpointToken upperBound)
        {
            Streams = streams;
            Reverse = reverse;
            EventNamesFilter = eventNamesFilter;
            EventNamesIncluding = eventNamesIncluding;
            CorrelationId = correlationId;
            LowerBound = lowerBound;
            UpperBound = upperBound;
        }

        public IReadOnlyList<StreamIdentifier> Streams { get; } 

        public bool Reverse { get; }

        public IReadOnlySet<string>? EventNamesFilter { get; }

        public bool EventNamesIncluding { get; }

        public string? CorrelationId { get; }

        public CheckpointToken LowerBound { get; }

        public CheckpointToken UpperBound { get; }
    }
}
