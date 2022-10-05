using System;

namespace MessageBus
{
    /// <summary>
    /// Uniquely identifies a single request. General rule of thumb:
    /// <list type="bullet">
    ///     <item>Commands and queries create a new MessageId by using <see cref="NewId"/> or use <see cref="CausationId"/> when having an
    ///     existing CorrelationId within a Command or Query handler.</item>
    ///     <item>If you create Commands because of an event, create a new CorrelationId by using <see cref="CausedBy(MessageId)"/> and
    ///     pass the MessageId of the event.</item>
    /// </list>
    /// </summary>
    public readonly struct MessageId : IEquatable<MessageId>
    {
        private readonly string? _causationId;

        public MessageId(string value)
            : this(value, null)
        {
        }

        private MessageId(string value, string? causationId)
        {
            if (string.IsNullOrEmpty(value)) throw new ArgumentException($"'{nameof(value)}' cannot be null or empty.", nameof(value));
            Value = value;
            _causationId = causationId;
        }

        public static MessageId NewId()
        {
            return new MessageId(Guid.NewGuid().ToString("N"));
        }

        public static MessageId CausedBy(MessageId messageId)
        {
            return new MessageId(Guid.NewGuid().ToString("N"), messageId.Value);
        }

        /// <summary>
        /// The raw value.
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// The correlationId which is the reason this message happened.
        /// Will be <c>null</c> if no causation id exists.
        /// </summary>
        public string? CausationId => _causationId;

        public bool Equals(MessageId other)
        {
            return Value.Equals(other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object? obj)
        {
            if (obj is not MessageId other)
                return false;
            return Equals(other);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode(StringComparison.Ordinal);
        }

        public override string ToString()
        {
            return _causationId is not null
                ? $"{_causationId}->{Value}"
                : Value.ToString();
        }

        public static bool operator ==(MessageId lhs, MessageId rhs) => lhs.Equals(rhs);

        public static bool operator !=(MessageId lhs, MessageId rhs) => !(lhs == rhs);
    }
}
