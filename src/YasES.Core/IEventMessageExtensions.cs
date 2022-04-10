using System.Diagnostics.CodeAnalysis;

namespace YasES.Core
{
    public static class IEventMessageExtensions
    {
        [return: NotNullIfNotNull("@default")]
        public static string? GetEventIdOrDefault(this IEventMessage @event, string? @default = default)
        {
            return @event.GetEventHeaderValueOrDefault(CommonMetaData.EventId, @default);
        }

        [return: NotNullIfNotNull("@default")]
        public static string? GetCorrelationIdOrDefault(this IEventMessage @event, string? @default = default)
        {
            return @event.GetEventHeaderValueOrDefault(CommonMetaData.CorrelationId, @default);
        }

        [return: NotNullIfNotNull("@default")]
        public static string? GetCausationIdOrDefault(this IEventMessage @event, string? @default = default)
        {
            return @event.GetEventHeaderValueOrDefault(CommonMetaData.CausationId, @default);
        }

        [return: NotNullIfNotNull("@default")]
        public static T? GetEventHeaderValueOrDefault<T>(this IEventMessage @event, string key, T? @default = default)
        {
            T? result = @default;
            if (@event.Headers.TryGetValue(key, out object? value) && value is not null && value is T)
            {
                result = (T)value;
            }
            return result;
        }
    }
}
