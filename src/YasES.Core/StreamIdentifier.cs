using System;

namespace YasES.Core
{
    public readonly struct StreamIdentifier : IEquatable<StreamIdentifier>
    {
        public static readonly string DefaultBucketId = "_default";
        private static readonly string StreamWildcard = "*";

        private StreamIdentifier(string bucketId, string streamId, string streamIdPrefix)
        {
            BucketId = bucketId;
            StreamId = streamId;
            StreamIdPrefix = streamIdPrefix;
        }

        /// <summary>
        /// Creates a stream identifier which points to exactly one stream. The
        /// stream is defined by <paramref name="streamId"/>. 
        /// <paramref name="bucketId"/> and <paramref name="streamId"/> are case sensitive.
        /// </summary>
        public static StreamIdentifier SingleStream(string bucketId, string streamId)
        {
            EnsureValidBucketId(bucketId);
            EnsureValidStreamId(streamId);
            return new StreamIdentifier(bucketId, streamId, streamIdPrefix: string.Empty);
        }

        /// <summary>
        /// Creates a stream identifier which points to all streams within the
        /// defined <paramref name="bucketId"/>. <paramref name="bucketId"/> is case sensitive.
        /// </summary>
        public static StreamIdentifier AllStreams(string bucketId)
        {
            EnsureValidBucketId(bucketId);
            return new StreamIdentifier(bucketId, StreamWildcard, streamIdPrefix: string.Empty);
        }

        /// <summary>
        /// Creates a stream identifier which points to all streams where each streamId
        /// starts with the provided <paramref name="streamIdPrefix"/>. <paramref name="bucketId"/>
        /// and <paramref name="streamIdPrefix"/> are case sensitive.
        /// </summary>
        public static StreamIdentifier StreamsPrefixedWith(string bucketId, string streamIdPrefix)
        {
            EnsureValidBucketId(bucketId);
            EnsureValidStreamId(streamIdPrefix);
            return new StreamIdentifier(bucketId, streamId: string.Empty, streamIdPrefix);
        }

        public static void EnsureValidBucketId(string bucketId)
        {
            if (string.IsNullOrWhiteSpace(bucketId)) throw new ArgumentException($"'{nameof(bucketId)}' cannot be null or whitespace.", nameof(bucketId));
            EnsureValidId(bucketId);
        }

        public static void EnsureValidStreamId(string streamId)
        {
            if (string.IsNullOrWhiteSpace(streamId)) throw new ArgumentException($"'{nameof(streamId)}' cannot be null or whitespace.", nameof(streamId));
            EnsureValidId(streamId);
        }

        private static void EnsureValidId(string id)
        {
            if (id.Trim() != id) throw new ArgumentException($"A stream must not start or end with whitespace characters, got '{id}'");
            if (id[0] == '$') throw new ArgumentException($"A stream must not start with an $, got '{id}'");
            if (id.Contains('*', StringComparison.Ordinal)) throw new ArgumentException($"A stream must not contain the '*' character, got '{id}'");
            if (id.Contains('\uFFFF', StringComparison.Ordinal)) throw new ArgumentException($"A stream must not contain the unicode 0xFFFF character, got '{id}'");
        }

        /// <summary>
        /// The bucket name the stream lives in.
        /// </summary>
        public string BucketId { get; }

        /// <summary>
        /// The name of the stream. <see cref="MatchesAllStreams"/> if the identifier
        /// is a wildcard identifier.
        /// </summary>
        public string StreamId { get; }

        /// <summary>
        /// The prefix to search for. Only use this property if <see cref="IsSingleStream"/>
        /// and <see cref="MatchesAllStreams"/> return false.
        /// </summary>
        public string StreamIdPrefix { get; }

        /// <summary>
        /// Returns true if <see cref="StreamId"/> points to all streams, otherwise false.
        /// </summary>
        public bool MatchesAllStreams => StreamId == StreamWildcard;

        /// <summary>
        /// Returns true if is a single stream identifier, otherwise false.
        /// </summary>
        public bool IsSingleStream => StreamId != StreamWildcard && StreamId != string.Empty;

        /// <summary>
        /// Returns true if the current identifier matches <paramref name="other"/>.
        /// </summary>
        public bool Matches(StreamIdentifier other)
        {
            if (!other.IsSingleStream)
                return false;
            if (BucketId != other.BucketId)
                return false;

            if (MatchesAllStreams)
                return true;

            if (IsSingleStream)
                return StreamId.Equals(other.StreamId, StringComparison.Ordinal);
            else
                return other.StreamId.StartsWith(StreamIdPrefix, StringComparison.Ordinal);
        }

        public bool Equals(StreamIdentifier other)
        {
            return BucketId == other.BucketId && StreamId == other.StreamId && StreamIdPrefix == other.StreamIdPrefix;
        }

        public override bool Equals(object? obj)
        {
            if (!(obj is StreamIdentifier other))
                return false;
            return Equals(other);
        }

        public override int GetHashCode()
        {
            return BucketId.GetHashCode() ^ StreamId.GetHashCode() ^ StreamIdPrefix.GetHashCode();
        }

        public override string ToString()
        {
            if (IsSingleStream || MatchesAllStreams)
                return $"{BucketId}/{StreamId}";
            return $"{BucketId}/{StreamId}*";
        }

        public static bool operator ==(StreamIdentifier lhs, StreamIdentifier rhs) => lhs.Equals(rhs);

        public static bool operator !=(StreamIdentifier lhs, StreamIdentifier rhs) => !(lhs.Equals(rhs));
    }
}
