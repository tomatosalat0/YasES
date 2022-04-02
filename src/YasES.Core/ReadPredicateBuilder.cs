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

        public static ReadPredicate Forwards(StreamIdentifier identifier)
        {
            return Custom()
                .FromSingleStream(identifier)
                .ReadForwards()
                .IncludeAllEvents()
                .WithoutCheckpointLimit()
                .Build();
        }

        public static ReadPredicate Forwards(StreamIdentifier identifier, params StreamIdentifier[] identifiers)
        {
            return Custom()
                .FromMultipleStreams((new[] { identifier }).Concat(identifiers))
                .ReadForwards()
                .IncludeAllEvents()
                .WithoutCheckpointLimit()
                .Build();
        }

        public static ReadPredicate Backwards(StreamIdentifier identifier)
        {
            return Custom()
                .FromSingleStream(identifier)
                .ReadBackwards()
                .IncludeAllEvents()
                .WithoutCheckpointLimit()
                .Build();
        }

        public static ReadPredicate Backwards(StreamIdentifier identifier, params StreamIdentifier[] identifiers)
        {
            return Custom()
                .FromMultipleStreams((new[] { identifier }).Concat(identifiers))
                .ReadBackwards()
                .IncludeAllEvents()
                .WithoutCheckpointLimit()
                .Build();
        }

        private class State : IAfterInit, IAfterStreams, IAfterDirection, IAfterEventFilter, ICompleted
        {
            private List<StreamIdentifier> _streams = new List<StreamIdentifier>();
            private bool _goBackwards = false;
            private IReadOnlySet<string>? _eventNames;
            private bool _eventNamesIncludes;
            private CheckpointToken _lowerBound;
            private CheckpointToken _upperBound;
            private string? _correlationId;

            public IAfterStreams FromSingleStream(StreamIdentifier identifier)
            {
                _streams.Add(identifier);
                return this;
            }

            public IAfterStreams FromMultipleStreams(IEnumerable<StreamIdentifier> identifiers)
            {
                _streams.AddRange(identifiers);
                return this;
            }

            public IAfterStreams FromMultipleStreams(params StreamIdentifier[] identifiers)
            {
                return FromMultipleStreams((IEnumerable<StreamIdentifier>)identifiers);
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
                return new ReadPredicate(_streams, _goBackwards, _eventNames, _eventNamesIncludes, _correlationId, _lowerBound, _upperBound);
            }
        }

        public interface IAfterInit
        {
            IAfterStreams FromSingleStream(StreamIdentifier identifier);

            IAfterStreams FromMultipleStreams(IEnumerable<StreamIdentifier> identifiers);

            IAfterStreams FromMultipleStreams(params StreamIdentifier[] identifiers);
        }

        public interface IAfterStreams
        {
            IAfterDirection ReadForwards();

            IAfterDirection ReadBackwards();
        }

        public interface IAfterDirection
        {
            IAfterEventFilter IncludeAllEvents();

            IAfterEventFilter OnlyIncluding(IReadOnlySet<string> eventNames);

            IAfterEventFilter AllExcluding(IReadOnlySet<string> excludingNames);
        }

        public interface IAfterEventFilter
        {
            ICompleted HavingTheCorrelationId(string correlationId);

            ICompleted WithoutCheckpointLimit();

            ICompleted RaisedAfterCheckpoint(CheckpointToken token);

            ICompleted RaisedBeforeCheckpoint(CheckpointToken token);

            ICompleted RaisedBetweenCheckpoints(CheckpointToken lowerBoundExclusive, CheckpointToken upperBoundExclusive);
        }

        public interface ICompleted
        {
            ReadPredicate Build();
        }
    }

    public static class IAfterInitExtensions
    {
        public static ReadPredicateBuilder.IAfterStreams FromAllStreamsInBucket(this ReadPredicateBuilder.IAfterInit builder, string bucketId)
        {
            return builder.FromSingleStream(StreamIdentifier.AllStreams(bucketId));
        }
    }

    public static class IAfterDirectionExtensions
    {
        public static ReadPredicateBuilder.IAfterEventFilter OnlyIncluding(this ReadPredicateBuilder.IAfterDirection builder, string eventName)
        {
            if (eventName is null) throw new ArgumentNullException(nameof(eventName));
            return builder.OnlyIncluding(new HashSet<string>() { eventName });
        }

        public static ReadPredicateBuilder.IAfterEventFilter OnlyIncluding(this ReadPredicateBuilder.IAfterDirection builder, string eventName, params string[] otherNames)
        {
            if (eventName is null) throw new ArgumentNullException(nameof(eventName));
            HashSet<string> set = new HashSet<string>(otherNames);
            set.Add(eventName);
            return builder.OnlyIncluding(set);
        }

        public static ReadPredicateBuilder.IAfterEventFilter AllExcluding(this ReadPredicateBuilder.IAfterDirection builder, string eventName)
        {
            if (eventName is null) throw new ArgumentNullException(nameof(eventName));
            return builder.AllExcluding(new HashSet<string>() { eventName });
        }

        public static ReadPredicateBuilder.IAfterEventFilter AllExcluding(this ReadPredicateBuilder.IAfterDirection builder, string eventName, params string[] otherNames)
        {
            if (eventName is null) throw new ArgumentNullException(nameof(eventName));
            HashSet<string> set = new HashSet<string>(otherNames);
            set.Add(eventName);
            return builder.AllExcluding(set);
        }
    }
}
