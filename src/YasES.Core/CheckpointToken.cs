using System;
using System.Globalization;

namespace YasES.Core
{
    public readonly struct CheckpointToken : IEquatable<CheckpointToken>, IComparable<CheckpointToken>
    {
        /// <summary>
        /// Defines the first checkpoint token of the persistance store.
        /// </summary>
        public static readonly CheckpointToken Beginning = new CheckpointToken(0);

        /// <summary>
        /// Defines the last possible checkpoint token for the persistance store.
        /// </summary>
        public static readonly CheckpointToken Ending = new CheckpointToken(long.MaxValue);

        public CheckpointToken(long value)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException($"The checkpoint token must not be negative, got {value}");
            Value = value;
        }

        public long Value { get; }

        public bool Equals(CheckpointToken other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object? obj)
        {
            if (!(obj is CheckpointToken other))
                return false;
            return Equals(other);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override string ToString()
        {
            return Value.ToString(CultureInfo.InvariantCulture);
        }

        public int CompareTo(CheckpointToken other)
        {
            return Value.CompareTo(other.Value);
        }

        public static CheckpointToken Parse(string value)
        {
            return new CheckpointToken(long.Parse(value, NumberStyles.None, CultureInfo.InvariantCulture));
        }

        public static CheckpointToken Parse(ReadOnlySpan<char> value)
        {
            return new CheckpointToken(long.Parse(value, NumberStyles.None, CultureInfo.InvariantCulture));
        }

        public static bool TryParse(string value, out CheckpointToken token)
        {
            token = default;
            if (!long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out long result))
                return false;

            token = new CheckpointToken(result);
            return true;
        }

        public static bool TryParse(ReadOnlySpan<char> value, out CheckpointToken token)
        {
            token = default;
            if (!long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out long result))
                return false;

            token = new CheckpointToken(result);
            return true;
        }

        public static bool operator ==(CheckpointToken lhs, CheckpointToken rhs) => lhs.Equals(rhs);
        public static bool operator !=(CheckpointToken lhs, CheckpointToken rhs) => !(lhs.Equals(rhs));
        public static bool operator >(CheckpointToken lhs, CheckpointToken rhs) => lhs.Value > rhs.Value;
        public static bool operator >=(CheckpointToken lhs, CheckpointToken rhs) => lhs.Value >= rhs.Value;
        public static bool operator <(CheckpointToken lhs, CheckpointToken rhs) => lhs.Value < rhs.Value;
        public static bool operator <=(CheckpointToken lhs, CheckpointToken rhs) => lhs.Value <= rhs.Value;
        public static CheckpointToken operator +(CheckpointToken lhs, long rhs) => new CheckpointToken(lhs.Value + rhs);
        public static CheckpointToken operator +(CheckpointToken lhs, CheckpointToken rhs) => new CheckpointToken(lhs.Value + rhs.Value);
        public static CheckpointToken operator +(long lhs, CheckpointToken rhs) => new CheckpointToken(lhs + rhs.Value);
        public static CheckpointToken operator -(CheckpointToken lhs, long rhs) => new CheckpointToken(lhs.Value - rhs);
        public static CheckpointToken operator -(CheckpointToken lhs, CheckpointToken rhs) => new CheckpointToken(lhs.Value - rhs.Value);
        public static CheckpointToken operator -(long lhs, CheckpointToken rhs) => new CheckpointToken(lhs - rhs.Value);
    }
}
