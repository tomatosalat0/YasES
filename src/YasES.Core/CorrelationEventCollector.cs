using System;
using System.Collections.Generic;
using System.Linq;

namespace YasES.Core
{
    /// <summary>
    /// All collected events will get the defined correlation id set in their
    /// header. If a passed event already contains a correlation id, an exception
    /// is thrown if the correlation id are not equal.
    /// </summary>
    public class CorrelationEventCollector : IEventCollector
    {
        private readonly IEventCollector _inner;
        private readonly string _correlationId;

        public CorrelationEventCollector(string correlationId, IEventCollector inner)
        {
            _correlationId = correlationId ?? throw new ArgumentNullException(nameof(correlationId));
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public bool IsEmpty => _inner.IsEmpty;

        public bool IsCommited => _inner.IsCommited;

        public IEventCollector Add(IEnumerable<IEventMessage> messages)
        {
            return _inner.Add(messages.Select(Map));
        }

        public CommitAttempt BuildCommit(StreamIdentifier stream, CommitId commitId)
        {
            return _inner.BuildCommit(stream, commitId);
        }

        private IEventMessage Map(IEventMessage message)
        {
            return new CorrelationAdjustedMessage(message, _correlationId);
        }

        private class CorrelationAdjustedMessage : IEventMessage
        {
            private readonly IEventMessage _message;

            public CorrelationAdjustedMessage(IEventMessage message, string correlationId)
            {
                _message = message;

                string? existingCorrelationId = message.Headers.GetValueOrDefault(CommonMetaData.CorrelationId) as string;
                if (existingCorrelationId != null)
                {
                    if (existingCorrelationId != correlationId)
                        throw new InvalidOperationException($"The added message has a different correlation id. Expected '{correlationId}', found '{existingCorrelationId}'");
                    Headers = message.Headers;
                } 
                else
                {
                    Headers = BuildWithCorrelationId(message.Headers, correlationId);
                }
            }

            private static IReadOnlyDictionary<string, object> BuildWithCorrelationId(IReadOnlyDictionary<string, object> source, string correlationId)
            {
                Dictionary<string, object> result = new Dictionary<string, object>(source.Count + 1);
                // can be hot path - reduces several memory allocations.
                if (source.Count > 0)
                {
                    foreach (var pair in source)
                        result[pair.Key] = pair.Value;
                }
                result[CommonMetaData.CorrelationId] = correlationId;
                return result;
            }

            public string EventName => _message.EventName;

            public IReadOnlyDictionary<string, object> Headers { get; }

            public ReadOnlyMemory<byte> Payload => _message.Payload;

            public DateTime CreationDateUtc => _message.CreationDateUtc;
        }
    }
}
