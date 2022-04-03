using System;
using System.Collections.Generic;

namespace YasES.Core
{
    public abstract class EventHeaderDecorator
    {
        private readonly string _key;
        private readonly Func<object> _valueFactory;

        protected EventHeaderDecorator(string key, Func<object> valueFactory)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentException($"'{nameof(key)}' cannot be null or empty.", nameof(key));
            _key = key;
            _valueFactory = valueFactory ?? throw new ArgumentNullException(nameof(valueFactory));
        }

        protected IEventMessage Decorate(IEventMessage message)
        {
            object value = _valueFactory();
            if (message.Headers.ContainsKey(_key))
            {
                return message;
            }
            return new DecoratedMessage(message, BuildWithHeader(message.Headers, _key, value));
        }

        private static IReadOnlyDictionary<string, object> BuildWithHeader(IReadOnlyDictionary<string, object> source, string key, object value)
        {
            Dictionary<string, object> result = new Dictionary<string, object>(source.Count + 1);
            // can be hot path - reduces several memory allocations.
            if (source.Count > 0)
            {
                foreach (var pair in source)
                    result[pair.Key] = pair.Value;
            }
            result[key] = value;
            return result;
        }

        private class DecoratedMessage : IEventMessage
        {
            private readonly IEventMessage _message;

            public DecoratedMessage(IEventMessage message, IReadOnlyDictionary<string, object> headers)
            {
                _message = message;
                Headers = headers ?? throw new ArgumentNullException(nameof(headers));
            }

            public string EventName => _message.EventName;

            public IReadOnlyDictionary<string, object> Headers { get; }

            public ReadOnlyMemory<byte> Payload => _message.Payload;

            public DateTime CreationDateUtc => _message.CreationDateUtc;
        }
    }
}
