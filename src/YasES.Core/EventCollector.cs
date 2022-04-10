using System;
using System.Collections.Generic;

namespace YasES.Core
{
    /// <summary>
    /// A non-threadsafe simple event collector which simplifies the creation of
    /// a <see cref="CommitAttempt"/>.
    /// </summary>
    public class EventCollector : IEventCollector
    {
        private readonly List<IEventMessage> _messages = new List<IEventMessage>();
        private bool _commited;

        /// <summary>
        /// Returns true if no messages has been added yet, otherwise false.
        /// </summary>
        public bool IsEmpty => _messages.Count == 0;

        /// <summary>
        /// Returns true if <see cref="BuildCommit(StreamIdentifier, CommitId)"/> has been executed, otherwise
        /// false.
        /// </summary>
        public bool IsCommited => _commited;

        /// <summary>
        /// Adds the provided <paramref name="messages"/> to the list of messages
        /// to commit.
        /// </summary>
        /// <exception cref="InvalidOperationException">Is thrown if <see cref="BuildCommit(StreamIdentifier, CommitId)"/> has been called before.</exception>
        public IEventCollector Add(IEnumerable<IEventMessage> messages)
        {
            EnsureNotCommited();
            _messages.AddRange(messages);
            return this;
        }

        /// <summary>
        /// Collects all previously added messages and adds them to a single
        /// <see cref="CommitAttempt"/>. You can only call this method once.
        /// </summary>
        public CommitAttempt BuildCommit(StreamIdentifier stream, CommitId commitId)
        {
            EnsureNotCommited();
            EnsureNotEmpty();
            CommitAttempt result = new CommitAttempt(stream, commitId, _messages);
            _commited = true;
            return result;
        }

        private void EnsureNotEmpty()
        {
            if (IsEmpty) throw new InvalidOperationException($"An empty collector can not be used to create a commit");
        }

        private void EnsureNotCommited()
        {
            if (_commited) throw new InvalidOperationException($"This collector has already been commited");
        }
    }
}
