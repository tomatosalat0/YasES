using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace YasES.Core
{
    /// <summary>
    /// Defines the filters to apply when reading from
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
        /// all streams must be in the same bucket.
        /// </summary>
        public IReadOnlyList<StreamIdentifier> Streams { get; }

        /// <summary>
        /// True if the events get returned in the reverse
        /// order they were created. If false, the events
        /// get returned in the order they where added to the streams.
        /// </summary>
        public bool Reverse { get; }

        /// <summary>
        /// Optional: if null, no filtering based on the <see cref="IEventMessage.EventName"/>
        /// will happen. If set, the list will
        /// contain the exact event names to filter by.
        /// See <see cref="EventNamesIncluding"/> if the
        /// filter is "include" or "exclude".
        /// </summary>
        public IReadOnlySet<string>? EventNamesFilter { get; }

        /// <summary>
        /// If true, only the events where <see cref="IEventMessage.EventName"/> is in <see cref="EventNamesFilter"/>
        /// are included in the result. If false, all events
        /// except those having <see cref="IEventMessage.EventName"/> in <see cref="EventNamesFilter"/>
        /// are be returned.
        /// </summary>
        public bool EventNamesIncluding { get; }

        /// <summary>
        /// Optional: if null, this property will get ignored. If defined,
        /// only events having the same correlation id (specified by the key
        /// <see cref="CommonMetaData.CorrelationId"/> within <see cref="IEventMessage.Headers"/>)
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
