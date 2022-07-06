using System;

namespace MCOM.Extensions
{
    public static partial class DateTimeExtensions
    {
        public static DateTime TryParseToLocalTime(this DateTime timeUtc)
        {
            try
            {
                return timeUtc.ToLocalTime();
            }
            catch (FormatException)
            {
                return timeUtc;
            }
        }

        public static DateTime TryParseToCetTime(this DateTime timeUtc)
        {
            try
            {
                var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");
                return TimeZoneInfo.ConvertTimeFromUtc(timeUtc, timeZone);
            }
            catch (FormatException)
            {
                return timeUtc;
            }            
        }
    }
}
