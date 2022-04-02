using System;

namespace YasES.Core
{
    public static class JsonSerialization
    {
        public static byte[] Serialize<T>(T value)
        {
            return Utf8Json.JsonSerializer.Serialize(value, Utf8Json.Resolvers.StandardResolver.AllowPrivateExcludeNullCamelCase);
        }

        public static T Deserialize<T>(in ReadOnlyMemory<byte> value)
        {
            return Utf8Json.JsonSerializer.Deserialize<T>(value.ToArray(), Utf8Json.Resolvers.StandardResolver.AllowPrivateExcludeNullCamelCase);
        }

        public static T Deserialize<T>(in ReadOnlySpan<byte> value)
        {
            return Utf8Json.JsonSerializer.Deserialize<T>(value.ToArray(), Utf8Json.Resolvers.StandardResolver.AllowPrivateExcludeNullCamelCase);
        }
    }
}
