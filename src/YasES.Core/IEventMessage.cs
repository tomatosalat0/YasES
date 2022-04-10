using System;
using System.Collections.Generic;

namespace YasES.Core
{
    public interface IEventMessage
    {
        /// <summary>
        /// Defines the name of the event.
        /// </summary>
        string EventName { get; }

        /// <summary>
        /// Contains meta-information of the event.
        /// </summary>
        IReadOnlyDictionary<string, object> Headers { get; }

        /// <summary>
        /// The payload of the event.
        /// </summary>
        ReadOnlyMemory<byte> Payload { get; }

        /// <summary>
        /// The point in time this instance was created on.
        /// </summary>
        DateTime CreationDateUtc { get; }
    }
}
