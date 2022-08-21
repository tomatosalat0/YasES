using System;
using MessageBus;

namespace YasES.Core
{
    [Topic("yases/events/events_commited")]
    public interface IAfterCommitEvent : IMessageEvent
    {
        CommitAttempt Attempt { get; }

        DateTime EventRaisedUtc { get; }
    }
}
