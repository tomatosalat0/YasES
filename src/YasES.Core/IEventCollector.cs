using System;
using System.Collections.Generic;

namespace YasES.Core
{
    public interface IEventCollector
    {
        /// <summary>
        /// Returns true if no messages has been added yet, otherwise false.
        /// </summary>
        bool IsEmpty { get; }

        /// <summary>
        /// Returns true if <see cref="BuildCommit(StreamIdentifier, CommitId)"/> has been executed, otherwise
        /// false.
        /// </summary>
        bool IsCommited { get; }

        /// <summary>
        /// Adds the provided <paramref name="messages"/> to the list of messages
        /// to commit.
        /// </summary>
        /// <exception cref="InvalidOperationException">Is thrown if <see cref="BuildCommit(StreamIdentifier, CommitId)"/> has been called before.</exception>
        IEventCollector Add(IEnumerable<IEventMessage> messages);

        /// <summary>
        /// Collects all previously added messages and adds them to a single
        /// <see cref="CommitAttempt"/>. You can only call this method once.
        /// </summary>
        CommitAttempt BuildCommit(StreamIdentifier stream, CommitId commitId);
    }

    public static class IEventCollectorExtensions
    {
        /// <summary>
        /// Creates the <see cref="CommitAttempt"/> with a generated <see cref="CommitId"/>.
        /// </summary>
        public static CommitAttempt BuildCommit(this IEventCollector collector, StreamIdentifier stream)
        {
            return collector.BuildCommit(stream, CommitId.NewId());
        }

        /// <summary>
        /// Adds the provided <paramref name="message"/> to the list of messages
        /// to commit.
        /// </summary>
        /// <exception cref="InvalidOperationException">Is thrown if <see cref="IEventCollector.BuildCommit(StreamIdentifier, CommitId)"/> has been called before.</exception>
        public static IEventCollector Add(this IEventCollector collector, IEventMessage message)
        {
            if (message is null) throw new ArgumentNullException(nameof(message));
            return collector.Add(new[] { message });
        }

        /// <summary>
        /// Adds the provided <paramref name="messages"/> to the list of messages
        /// to commit.
        /// </summary>
        /// <exception cref="InvalidOperationException">Is thrown if <see cref="IEventCollector.BuildCommit(StreamIdentifier, CommitId)"/> has been called before.</exception>
        public static IEventCollector Add(this IEventCollector collector, params IEventMessage[] messages)
        {
            return collector.Add((IEnumerable<IEventMessage>)messages);
        }
    }
}
