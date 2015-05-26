using System;

namespace Nano.Tests
{
    /// <summary>
    /// TimeSpan Helper.
    /// </summary>
    public static class TimeSpanHelper
    {
        /// <summary>
        /// Returns a formatted string of the given timespan.
        /// </summary>
        /// <remarks>
        /// Supports up to microsecond resolution.
        /// </remarks>
        /// <example>
        /// 1 days 6 hours 52 min 34 sec 556 ms
        /// </example>
        /// <returns>A string with a customized output of the elapsed time.</returns>
        public static string GetFormattedTime( this TimeSpan timeSpan )
        {
            string elapsedTime;

            if( timeSpan.Days > 0 )
                elapsedTime = string.Format( "{0:%d} d {0:%h} hrs {0:%m} min {0:%s} sec {0:%fff} ms", timeSpan );
            else if( timeSpan.Hours > 0 )
                elapsedTime = string.Format( "{0:%h} hrs {0:%m} min {0:%s} sec {0:%fff} ms", timeSpan );
            else if( timeSpan.Minutes > 0 )
                elapsedTime = string.Format( "{0:%m} min {0:%s} sec {0:%fff} ms", timeSpan );
            else if( timeSpan.Seconds > 0 )
                elapsedTime = string.Format( "{0:%s} sec {0:%fff} ms", timeSpan );
            else if( timeSpan.TotalMilliseconds > 0.9999999 )
                elapsedTime = string.Format( "{0:%fff} ms", timeSpan );
            else
                elapsedTime = string.Format( "{0} µs", timeSpan.TotalMilliseconds * 1000.0 );

            return elapsedTime;
        }
    }
}