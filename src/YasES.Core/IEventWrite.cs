using System;
using System.Collections.Generic;

namespace YasES.Core
{
    public interface IEventWrite
    {
        /// <summary>
        /// Saves the provided <paramref name="attempt"/> to the persistant store.
        /// </summary>
        void Commit(CommitAttempt attempt);
    }

    public sealed class CommitAttempt
    {
        public CommitAttempt(StreamIdentifier streamIdentifier, CommitId commitId, IReadOnlyList<IEventMessage> messages)
        {
            if (messages is null) throw new ArgumentNullException(nameof(messages));
            if (messages.Count == 0) throw new ArgumentException("You must provide at least one message");
            if (!streamIdentifier.IsSingleStream) throw new ArgumentException($"The stream identifier must point to a single stream");
            StreamIdentifier = streamIdentifier;
            CommitId = commitId;
            Messages = messages;
        }

        public StreamIdentifier StreamIdentifier { get; }

        public CommitId CommitId { get; }

        public IReadOnlyList<IEventMessage> Messages { get; }

        public DateTime CommitAttemptDateUtc { get; } = SystemClock.UtcNow;
    }
}
