using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace YasES.Core
{
    /// <summary>
    /// Defines the operation and conditions to execute when reading from 
    /// the event store. Note that a single stream predicate can not
    /// request streams from multiple buckets simultaniously.
    /// </summary>
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
            LowerExclusiveBound = lowerBound;
            UpperExclusiveBound = upperBound;
        }

        /// <summary>
        /// The streams to return within this request. Note that
        /// all streams will be in the same bucket.
        /// </summary>
        public IReadOnlyList<StreamIdentifier> Streams { get; } 

        /// <summary>
        /// True if the events should get returned in the reverse 
        /// order they were created. If false, the events should
        /// get returned in the order they where added to the streams.
        /// </summary>
        public bool Reverse { get; }

        /// <summary>
        /// Optional: if null, no filtering based on the event
        /// names should happen. If set, the list will 
        /// contain the exact event names to filter by. 
        /// See <see cref="EventNamesIncluding"/> if the
        /// filter is "include" or "exclude".
        /// </summary>
        public IReadOnlySet<string>? EventNamesFilter { get; }

        /// <summary>
        /// If true, only the events defined in <see cref="EventNamesFilter"/>
        /// should be included in the result. If false, all events
        /// exect those having one of the specified names in <see cref="EventNamesFilter"/>
        /// should be returned.
        /// </summary>
        public bool EventNamesIncluding { get; }

        /// <summary>
        /// Optional: if null, this property will get ignored. If defined,
        /// only events having the specified correlationId in their header should
        /// get returned.
        /// </summary>
        public string? CorrelationId { get; }

        /// <summary>
        /// Defines the exlusive lower bound for the checkpoint token. Only
        /// events which have a checkpoint token greater than this value
        /// will get returned. The lowest value will be <see cref="CheckpointToken.Beginning"/>.
        /// </summary>
        public CheckpointToken LowerExclusiveBound { get; }

        /// <summary>
        /// Defines the exlusive upper bound for the checkpoint token. Only
        /// events which have a checkpoint token smaller than this value
        /// will get returned. The highest value will be <see cref="CheckpointToken.Ending"/>.
        /// </summary>
        public CheckpointToken UpperExclusiveBound { get; }
    }
}
