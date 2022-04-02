using System.Collections.Generic;
using YasES.Core;

namespace YasES.Examples.PersonList
{
    public static class IReadEventMessageExtensions
    {
        public static IReadOnlyDictionary<string, object> PayloadData(this IEventMessage message)
        {
            return JsonSerialization.Deserialize<IReadOnlyDictionary<string, object>>(message.Payload.Span);
        }
    }
}
