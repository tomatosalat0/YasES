using System;
using System.Collections.Generic;
using System.Linq;

namespace YasES.Core
{
    public static class ReadPredicateBuilder
    {
        public static IAfterInit Custom()
        {
            return new State();
        }

        /// <summary>
        /// Returns all events which match the specified <paramref name="identifier"/>.
        /// </summary>
        public static ReadPredicate Forwards(StreamIdentifier identifier)
        {
            return Custom()
                .FromStream(identifier)
                .ReadForwards()
                .IncludeAllEvents()
                .WithoutCheckpointLimit()
                .Build();
        }

        /// <summary>
        /// Returns all events which match one of the specified <paramref name="identifier"/>.
        /// </summary>
        public static ReadPredicate Forwards(StreamIdentifier identifier, params StreamIdentifier[] identifiers)
        {
            return Custom()
                .FromStreams((new[] { identifier }).Concat(identifiers))
                .ReadForwards()
                .IncludeAllEvents()
                .WithoutCheckpointLimit()
                .Build();
        }

        /// <summary>
        /// Returns all events which match the specified <paramref name="identifier"/> in the reverse creation
        /// order.
        /// </summary>
        public static ReadPredicate Backwards(StreamIdentifier identifier)
        {
            return Custom()
                .FromStream(identifier)
                .ReadBackwards()
                .IncludeAllEvents()
                .WithoutCheckpointLimit()
                .Build();
        }

        /// <summary>
        /// Returns all events which match one of the specified <paramref name="identifier"/> in the reverse creation
        /// order.
        /// </summary>
        public static ReadPredicate Backwards(StreamIdentifier identifier, params StreamIdentifier[] identifiers)
        {
            return Custom()
                .FromStreams((new[] { identifier }).Concat(identifiers))
                .ReadBackwards()
                .IncludeAllEvents()
                .WithoutCheckpointLimit()
                .Build();
        }

        private class State : IAfterInit, IAfterStreams, IAfterDirection, IAfterEventFilter, ICompleted
        {
            private readonly List<StreamIdentifier> _streams = new List<StreamIdentifier>();
            private bool _goBackwards = false;
            private IReadOnlySet<string>? _eventNames;
            private bool _eventNamesIncludes;
            private CheckpointToken _lowerBound;
            private CheckpointToken _upperBound;
            private string? _correlationId;

            public IAfterStreams FromStream(StreamIdentifier identifier)
            {
                _streams.Add(identifier);
                return this;
            }

            public IAfterStreams FromStreams(IEnumerable<StreamIdentifier> identifiers)
            {
                _streams.AddRange(identifiers);
                return this;
            }

            public IAfterStreams FromStreams(params StreamIdentifier[] identifiers)
            {
                return FromStreams((IEnumerable<StreamIdentifier>)identifiers);
            }

            public IAfterDirection ReadBackwards()
            {
                _goBackwards = true;
                return this;
            }

            public IAfterDirection ReadForwards()
            {
                _goBackwards = false;
                return this;
            }

            public IAfterEventFilter IncludeAllEvents()
            {
                _eventNames = null;
                _eventNamesIncludes = false;
                return this;
            }

            public IAfterEventFilter OnlyIncluding(IReadOnlySet<string> eventNames)
            {
                if (eventNames.Count == 0) throw new ArgumentException("The event names list must not be empty");

                _eventNames = eventNames;
                _eventNamesIncludes = true;
                return this;
            }
            public IAfterEventFilter AllExcluding(IReadOnlySet<string> excludingNames)
            {
                if (excludingNames.Count == 0) throw new ArgumentException("The event names list must not be empty");

                _eventNames = excludingNames;
                _eventNamesIncludes = false;
                return this;
            }

            public ICompleted HavingTheCorrelationId(string correlationId)
            {
                _correlationId = correlationId ?? throw new ArgumentNullException(nameof(correlationId));
                return WithoutCheckpointLimit();
            }

            public ICompleted WithoutCheckpointLimit()
            {
                return RaisedBetweenCheckpoints(CheckpointToken.Beginning, CheckpointToken.Ending);
            }

            public ICompleted RaisedAfterCheckpoint(CheckpointToken token)
            {
                return RaisedBetweenCheckpoints(token, CheckpointToken.Ending);
            }

            public ICompleted RaisedBeforeCheckpoint(CheckpointToken token)
            {
                return RaisedBetweenCheckpoints(CheckpointToken.Beginning, token);
            }

            public ICompleted RaisedBetweenCheckpoints(CheckpointToken lowerBoundExclusive, CheckpointToken upperBoundExclusive)
            {
                _lowerBound = lowerBoundExclusive;
                _upperBound = upperBoundExclusive;
                return this;
            }

            public ReadPredicate Build()
            {
                if (_streams.Select(p => p.BucketId).Distinct().Count() > 1)
                    throw new InvalidOperationException($"A single read predicate must not contain streams from different buckets");
                return new ReadPredicate(_streams, _goBackwards, _eventNames, _eventNamesIncludes, _correlationId, _lowerBound, _upperBound);
            }
        }

        public interface IAfterInit
        {
            /// <summary>
            /// Returns events where the stream id matches <paramref name="identifier"/>.
            /// </summary>
            IAfterStreams FromStream(StreamIdentifier identifier);

            /// <summary>
            /// Returns events where the stream id matches any of the provided <paramref name="identifiers"/>.
            /// </summary>
            IAfterStreams FromStreams(IEnumerable<StreamIdentifier> identifiers);

            /// <summary>
            /// Returns events where the stream id matches any of the provided <paramref name="identifiers"/>.
            /// </summary>
            IAfterStreams FromStreams(params StreamIdentifier[] identifiers);
        }

        public interface IAfterStreams
        {
            /// <summary>
            /// The events are returned in the order they were commited.
            /// </summary>
            IAfterDirection ReadForwards();

            /// <summary>
            /// The events are returned in the reverse order they were commited.
            /// </summary>
            IAfterDirection ReadBackwards();
        }

        public interface IAfterDirection
        {
            /// <summary>
            /// No filtering in <see cref="IEventMessage.EventName"/> will get used.
            /// </summary>
            IAfterEventFilter IncludeAllEvents();

            /// <summary>
            /// Only events where <see cref="IEventMessage.EventName"/> is within the provided set
            /// of allowed <paramref name="eventNames"/>.
            /// </summary>
            IAfterEventFilter OnlyIncluding(IReadOnlySet<string> eventNames);

            /// <summary>
            /// Only events where <see cref="IEventMessage.EventName"/> is not within the provided set
            /// of <paramref name="excludingNames"/>.
            /// </summary>
            IAfterEventFilter AllExcluding(IReadOnlySet<string> excludingNames);
        }

        public interface IAfterEventFilter
        {
            /// <summary>
            /// Only include events where the <see cref="IEventMessage.Headers"/> contains a <see cref="CommonMetaData.CorrelationId"/>
            /// and the value inside that header is equal to <paramref name="correlationId"/>.
            /// </summary>
            ICompleted HavingTheCorrelationId(string correlationId);

            /// <summary>
            /// No further filtering is used.
            /// </summary>
            ICompleted WithoutCheckpointLimit();

            /// <summary>
            /// Only events which where saved after the provided <paramref name="token"/> are returned.
            /// </summary>
            ICompleted RaisedAfterCheckpoint(CheckpointToken token);

            /// <summary>
            /// Only events which where saved before the provided <paramref name="token"/> are returned.
            /// </summary>
            ICompleted RaisedBeforeCheckpoint(CheckpointToken token);

            /// <summary>
            /// Only events which where saved between the <paramref name="lowerBoundExclusive"/> and <paramref name="upperBoundExclusive"/>.
            /// </summary>
            ICompleted RaisedBetweenCheckpoints(CheckpointToken lowerBoundExclusive, CheckpointToken upperBoundExclusive);
        }

        public interface ICompleted
        {
            /// <summary>
            /// Create the <see cref="ReadPredicate"/> which can get passed to <see cref="IEventRead.Read(ReadPredicate)"/>.
            /// </summary>
            ReadPredicate Build();
        }
    }

    public static class IAfterInitExtensions
    {
        /// <summary>
        /// Include all events of all streams which are within the provided <paramref name="bucketId"/>.
        /// </summary>
        public static ReadPredicateBuilder.IAfterStreams FromAllStreamsInBucket(this ReadPredicateBuilder.IAfterInit builder, string bucketId)
        {
            return builder.FromStream(StreamIdentifier.AllStreams(bucketId));
        }
    }

    public static class IAfterDirectionExtensions
    {
        /// <summary>
        /// Only events where <see cref="IEventMessage.EventName"/> is equal to <paramref name="eventName"/> are returned.
        /// </summary>
        public static ReadPredicateBuilder.IAfterEventFilter OnlyIncluding(this ReadPredicateBuilder.IAfterDirection builder, string eventName)
        {
            if (eventName is null) throw new ArgumentNullException(nameof(eventName));
            return builder.OnlyIncluding(new HashSet<string>() { eventName });
        }

        /// <summary>
        /// Only events where <see cref="IEventMessage.EventName"/> is equal to one of <paramref name="eventName"/> are returned.
        /// </summary>
        public static ReadPredicateBuilder.IAfterEventFilter OnlyIncluding(this ReadPredicateBuilder.IAfterDirection builder, string eventName, params string[] otherNames)
        {
            if (eventName is null) throw new ArgumentNullException(nameof(eventName));
            HashSet<string> set = new HashSet<string>(otherNames);
            set.Add(eventName);
            return builder.OnlyIncluding(set);
        }

        /// <summary>
        /// Only events where <see cref="IEventMessage.EventName"/> is not equal to <paramref name="eventName"/> are returned.
        /// </summary>
        public static ReadPredicateBuilder.IAfterEventFilter AllExcluding(this ReadPredicateBuilder.IAfterDirection builder, string eventName)
        {
            if (eventName is null) throw new ArgumentNullException(nameof(eventName));
            return builder.AllExcluding(new HashSet<string>() { eventName });
        }

        /// <summary>
        /// Only events where <see cref="IEventMessage.EventName"/> is not equal to one of the <paramref name="eventName"/> are returned.
        /// </summary>
        public static ReadPredicateBuilder.IAfterEventFilter AllExcluding(this ReadPredicateBuilder.IAfterDirection builder, string eventName, params string[] otherNames)
        {
            if (eventName is null) throw new ArgumentNullException(nameof(eventName));
            HashSet<string> set = new HashSet<string>(otherNames);
            set.Add(eventName);
            return builder.AllExcluding(set);
        }
    }
}
