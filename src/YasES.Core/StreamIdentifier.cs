using System;

namespace YasES.Core
{
    public readonly struct StreamIdentifier : IEquatable<StreamIdentifier>
    {
        public static readonly string DefaultBucketId = "_default";
        private static readonly string StreamWildcard = "*";

        private StreamIdentifier(string bucketId, string streamId)
        {
            BucketId = bucketId;
            StreamId = streamId;
        }

        public static StreamIdentifier SingleStream(string bucketId, string streamId)
        {
            EnsureValidBucketId(bucketId);
            EnsureValidStreamId(streamId);
            return new StreamIdentifier(bucketId, streamId);
        }

        public static StreamIdentifier AllStreams(string bucketId)
        {
            EnsureValidBucketId(bucketId);
            return new StreamIdentifier(bucketId, StreamWildcard);
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
        /// Returns true if <see cref="StreamId"/> points to all streams, otherwise false.
        /// </summary>
        public bool MatchesAllStreams => StreamId == StreamWildcard;

        public bool Equals(StreamIdentifier other)
        {
            return BucketId == other.BucketId && StreamId == other.StreamId;
        }

        public override bool Equals(object? obj)
        {
            if (!(obj is StreamIdentifier other))
                return false;
            return Equals(other);
        }

        public override int GetHashCode()
        {
            return BucketId.GetHashCode() ^ StreamId.GetHashCode();
        }

        public override string ToString()
        {
            return $"{BucketId}/{StreamId}";
        }

        public static bool operator ==(StreamIdentifier lhs, StreamIdentifier rhs) => lhs.Equals(rhs);

        public static bool operator !=(StreamIdentifier lhs, StreamIdentifier rhs) => !(lhs.Equals(rhs));
    }
}
