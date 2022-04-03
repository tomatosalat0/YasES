using System;
using System.Collections.Generic;

namespace YasES.Core.Persistance
{
    public static class HeaderSerialization
    {
        public static byte[] HeaderToJson(IEventMessage message)
        {
            return JsonSerialization.Serialize(message.Headers);
        }

#pragma warning disable SA1011 // Closing square brackets should be spaced correctly
        public static IReadOnlyDictionary<string, object> DeserializeHeaderJsonOrDefault(byte[]? data)
#pragma warning restore SA1011 // Closing square brackets should be spaced correctly
        {
            if (data == null)
                return new Dictionary<string, object>();
            return JsonSerialization.Deserialize<Dictionary<string, object>>(data.AsSpan());
        }
    }
}
