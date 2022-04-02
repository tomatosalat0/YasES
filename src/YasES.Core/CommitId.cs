using System;

namespace YasES.Core
{
    public readonly struct CommitId : IEquatable<CommitId>
    {
        public CommitId(Guid value)
        {
            if (value == Guid.Empty) throw new ArgumentException($"The commit id must not be Guid.Empty");
            Value = value;
        }

        public static CommitId NewId()
        {
            return new CommitId(Guid.NewGuid());
        }

        public Guid Value { get; }

        public bool Equals(CommitId other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object? obj)
        {
            if (!(obj is CommitId other))
                return false;
            return Equals(other);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public static bool operator ==(CommitId lhs, CommitId rhs) => lhs.Equals(rhs);

        public static bool operator !=(CommitId lhs, CommitId rhs) => !(lhs.Equals(rhs));

        public static CommitId Parse(string value)
        {
            return new CommitId(Guid.Parse(value));
        }

        public static CommitId Parse(ReadOnlySpan<char> value)
        {
            return new CommitId(Guid.Parse(value));
        }

        public static bool TryParse(string value, out CommitId token)
        {
            token = default;
            if (!Guid.TryParse(value, out Guid result) || result == Guid.Empty)
                return false;

            token = new CommitId(result);
            return true;
        }

        public static bool TryParse(ReadOnlySpan<char> value, out CommitId token)
        {
            token = default;
            if (!Guid.TryParse(value, out Guid result) || result == Guid.Empty)
                return false;

            token = new CommitId(result);
            return true;
        }
    }
}
