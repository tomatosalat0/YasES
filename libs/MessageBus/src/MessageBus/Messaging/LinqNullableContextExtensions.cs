using System.Collections.Generic;
using System.Linq;

namespace MessageBus.Messaging
{
    /// <summary>
    /// Required extension to keep nullable-context enable and to remove
    /// the null warnings (see https://stackoverflow.com/a/58373257/42921).
    /// </summary>
    public static class LinqNullableContextExtensions
    {
        public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> o) where T : class
        {
            return o.Where(x => x != null)!;
        }

        public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> o) where T : struct
        {
            return o.Where(x => x != null).Select(x => x!.Value);
        }
    }
}
