using System;
using System.Collections.Generic;
using System.Linq;
using YasES.Core;

namespace YasES.Persistance.InMemory
{
    internal class MessageListContainer
    {
        private readonly List<IReadEventMessage> _messages = new List<IReadEventMessage>();
        private readonly object _messagesLock = new object();

        public MessageListContainer(StreamIdentifier identifier)
        {
            Identifier = identifier;
        }

        public StreamIdentifier Identifier { get; }

        internal long Append(CommitAttempt attempt, long nextSequenceId)
        {
            lock(_messagesLock)
            {
                DateTime commitTime = SystemClock.UtcNow;
                foreach (var message in attempt.Messages)
                {
                    if (attempt.StreamIdentifier != Identifier)
                        throw new InvalidOperationException($"A message container can only accept messages of the same type, expected '{Identifier}', got '{attempt.StreamIdentifier}'");
                    _messages.Add(new MemoryReadMessage(message, attempt.CommitId, attempt.StreamIdentifier, new CheckpointToken(nextSequenceId), commitTime));
                    nextSequenceId++;
                }
            }
            return nextSequenceId;
        }

        internal IReadOnlyList<IReadEventMessage> CreateSnapshot(Func<IReadEventMessage, bool> predicate)
        {
            lock(_messagesLock)
            {
                return _messages.Where(predicate).ToList();
            }
        }

        private class MemoryReadMessage : IReadEventMessage
        {
            private readonly IEventMessage _message;

            public MemoryReadMessage(IEventMessage message, CommitId commitId, StreamIdentifier streamIdentifier, CheckpointToken checkpoint, DateTime commitTimeUtc)
            {
                _message = message;
                CreationDateUtc = message.CreationDateUtc;
                CommitId = commitId;
                StreamIdentifier = streamIdentifier;
                Checkpoint = checkpoint;
                CommitTimeUtc = commitTimeUtc;
            }

            public CommitId CommitId { get; }

            public StreamIdentifier StreamIdentifier { get; }

            public CheckpointToken Checkpoint { get; }

            public DateTime CommitTimeUtc { get; }

            public string EventName => _message.EventName;

            public IReadOnlyDictionary<string, object> Headers => _message.Headers;

            public ReadOnlyMemory<byte> Payload => _message.Payload;

            public DateTime CreationDateUtc { get; }
        }
    }
}
