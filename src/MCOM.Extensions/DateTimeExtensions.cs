using System;

namespace MCOM.Extensions
{
    public static partial class DateTimeExtensions
    {
        public static DateTime TryParseToLocalTime(this DateTime univDateTime)
        {
            try
            {
                return univDateTime.ToLocalTime();
            }
            catch (FormatException)
            {
                return univDateTime;
            }
        }
    }
}
