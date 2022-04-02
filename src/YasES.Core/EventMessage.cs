using System;
using System.Collections.Generic;

namespace YasES.Core
{
    public sealed class EventMessage : IEventMessage
    {
        private static readonly IReadOnlyDictionary<string, object> DefaultHeaders = new Dictionary<string, object>();

        public EventMessage(string eventName, IReadOnlyDictionary<string, object> headers, ReadOnlyMemory<byte> payload)
        {
            if (string.IsNullOrWhiteSpace(eventName)) throw new ArgumentException($"'{nameof(eventName)}' cannot be null or whitespace.", nameof(eventName));
            EventName = eventName;
            Headers = headers;
            Payload = payload;
        }

        public EventMessage(string eventName, ReadOnlyMemory<byte> payload)
            : this(eventName, DefaultHeaders, payload)
        {
        }
        
        /// <inheritdoc/>
        public string EventName { get; }

        /// <inheritdoc/>
        public IReadOnlyDictionary<string, object> Headers { get; }

        /// <inheritdoc/>
        public ReadOnlyMemory<byte> Payload { get; }

        /// <inheritdoc/>
        public DateTime CreationDateUtc { get; } = SystemClock.UtcNow;
    }
}
