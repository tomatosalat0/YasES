using System;

namespace YasES.Core
{
    public interface IStoredEventMessage : IEventMessage
    {
        /// <summary>
        /// The checkpoint token within the persistance store.
        /// </summary>
        CheckpointToken Checkpoint { get; }

        /// <summary>
        /// The id of the commit this message has been added to the persistance store.
        /// </summary>
        CommitId CommitId { get; }

        /// <summary>
        /// Gets the stream identifier this message belongs to.
        /// </summary>
        StreamIdentifier StreamIdentifier { get; }

        /// <summary>
        /// The date+time value the message has been commited to the persistance store.
        /// </summary>
        DateTime CommitTimeUtc { get; }
    }
}
