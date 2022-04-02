using System;

namespace YasES.Core
{
    public static class SystemClock
    {
        /// <summary>
        /// The resolver for the system clock. Do not use the resolver
        /// directly, use <see cref="UtcNow"/>.
        /// </summary>
        public static Func<DateTime> ResolveUtcNow = () => DateTime.UtcNow;

        /// <summary>
        /// Returns the system utc time.
        /// </summary>
        public static DateTime UtcNow => ResolveUtcNow();
    }
}
