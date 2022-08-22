using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedisStatusPage.Core
{
    public static class DateTimeHelpers
    {
        public static string ToUnixFormat(this DateTime d)
        {
            return d.ToString("O");
        }

        public static DateTime FromUnixFormat(string s)
        {
            return DateTime.Parse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        }

        public static long ToUnixSeconds(this DateTime d)
        {
            return new DateTimeOffset(d.ToUniversalTime()).ToUnixTimeSeconds();
        }

        public static DateTime FromUnixSeconds(long seconds)
        {
            return DateTimeOffset.FromUnixTimeSeconds(seconds).LocalDateTime;
        }
    }
}
