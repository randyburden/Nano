using System;

namespace Nano.Demo
{
    /// <summary>
    /// Time API.
    /// </summary>
    public class Time
    {
        /// <summary>
        /// Gets the current date and time.
        /// </summary>
        /// <returns>Current date and time.</returns>
        public static DateTime GetCurrentDateAndTime()
        {
            return DateTime.Now;;
        }

        /// <summary>
        /// Gets the curent day of the week.
        /// </summary>
        /// <returns>Day of the week.</returns>
        public static string GetDayOfWeek()
        {
            return DateTime.Now.DayOfWeek.ToString();
        }
    }
}