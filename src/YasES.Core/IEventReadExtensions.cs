using System.Collections.Generic;

namespace YasES.Core
{
    public static class IEventReadExtensions
    {
        /// <summary>
        /// Returns a reader for the <paramref name="stream"/>, starting at <paramref name="lowerBoundExclusive"/> going to the future.
        /// </summary>
        public static IEnumerable<IStoredEventMessage> ReadForwardFrom(this IEventRead reader, StreamIdentifier stream, CheckpointToken lowerBoundExclusive)
        {
            return reader.Read(ReadPredicateBuilder.Custom()
                .FromStream(stream)
                .ReadForwards()
                .IncludeAllEvents()
                .RaisedAfterCheckpoint(lowerBoundExclusive)
                .Build());
        }

        /// <summary>
        /// Returns a reader for the <paramref name="stream"/>, starting at <paramref name="lowerBoundExclusive"/> going to the future.
        /// The reader will end as soon as <paramref name="upperBoundExclusive"/> has been returned or no more messages
        /// could be found inside the store.
        /// </summary>
        public static IEnumerable<IStoredEventMessage> ReadForwardFromTo(this IEventRead reader, StreamIdentifier stream, CheckpointToken lowerBoundExclusive, CheckpointToken upperBoundExclusive)
        {
            return reader.Read(ReadPredicateBuilder.Custom()
                .FromStream(stream)
                .ReadForwards()
                .IncludeAllEvents()
                .RaisedBetweenCheckpoints(lowerBoundExclusive, upperBoundExclusive)
                .Build());
        }

        /// <summary>
        /// Returns a reader for the <paramref name="stream"/>, starting at the <paramref name="upperBoundExclusive"/> going to the past.
        /// The reader will end as soon as the start of the stream has been reached.
        /// </summary>
        public static IEnumerable<IStoredEventMessage> ReadBackwardFrom(this IEventRead reader, StreamIdentifier stream, CheckpointToken upperBoundExclusive)
        {
            return reader.Read(ReadPredicateBuilder.Custom()
                .FromStream(stream)
                .ReadBackwards()
                .IncludeAllEvents()
                .RaisedBeforeCheckpoint(upperBoundExclusive)
                .Build());
        }

        /// <summary>
        /// Returns a reader for the <paramref name="stream"/>, starting at the <paramref name="lowerBoundExclusive"/> going to the past.
        /// The reader will end as soon as <paramref name="upperBoundExclusive"/> has been returned or the start of the stream has been reached.
        /// Note that <paramref name="lowerBoundExclusive"/> must be greater than <paramref name="upperBoundExclusive"/>.
        /// </summary>
        public static IEnumerable<IStoredEventMessage> ReadBackwardFromTo(this IEventRead reader, StreamIdentifier stream, CheckpointToken lowerBoundExclusive, CheckpointToken upperBoundExclusive)
        {
            return reader.Read(ReadPredicateBuilder.Custom()
                .FromStream(stream)
                .ReadBackwards()
                .IncludeAllEvents()
                .RaisedBetweenCheckpoints(lowerBoundExclusive, upperBoundExclusive)
                .Build());
        }
    }
}
