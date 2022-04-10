using System;
using System.Collections.Generic;

namespace YasES.Core
{
    public static class IEventWriteExtensions
    {
        public static void Commit(this IEventWrite writer, StreamIdentifier identifier, IReadOnlyList<IEventMessage> messages)
        {
            writer.Commit(new CommitAttempt(identifier, CommitId.NewId(), messages));
        }

        public static void Commit(this IEventWrite writer, StreamIdentifier identifier, IEventMessage message)
        {
            if (message is null) throw new ArgumentNullException(nameof(message));
            writer.Commit(new CommitAttempt(identifier, CommitId.NewId(), new[] { message }));
        }
    }
}
