using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace MessageBus.Messaging
{
    public readonly struct TopicName : IEquatable<TopicName>
    {
        private readonly string _value;

        public TopicName(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException($"'{nameof(value)}' cannot be null or whitespace.", nameof(value));
            ValidateNoWhitespace(value);
            _value = value;
        }

        public static TopicName Build(params string[] names)
        {
            if (names.Length == 0)
                throw new ValidationException($"You must provided at least one section name");
            return new TopicName(string.Join('/', names.Select(ValidateTopicSectionOrThrow)));
        }

        private static void ValidateNoWhitespace(string value)
        {
            foreach (char c in value)
            {
                if (char.IsWhiteSpace(c))
                    throw new ValidationException($"The topic section must not contain any whitespace, got '{value}'.");
            }
        }

        private static string ValidateTopicSectionOrThrow(string value)
        {
            if (string.IsNullOrEmpty(value))
                throw new ValidationException("The topic section must not be empty");

            foreach (char c in value)
            {
                if (char.IsWhiteSpace(c))
                    throw new ValidationException($"The topic section must not contain any whitespace, got '{value}'.");

                if (c == '/')
                    throw new ValidationException($"The topic section must not contain any / character, got '{value}'");
            }

            return value;
        }

        public bool Equals(TopicName other)
        {
            return _value.Equals(other._value, StringComparison.Ordinal);
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is not TopicName other)
                return false;
            return Equals(other);
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode(StringComparison.Ordinal);
        }

        public override string ToString()
        {
            return _value;
        }

        public static bool operator ==(TopicName lhs, TopicName rhs) => lhs.Equals(rhs);

        public static bool operator !=(TopicName lhs, TopicName rhs) => !(lhs == rhs);

        public static implicit operator TopicName(string value) => new TopicName(value);
    }
}
