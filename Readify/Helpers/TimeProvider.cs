using System;

namespace Readify.Helpers
{
    public static class SriLankaTimeProvider
    {
        private static TimeZoneInfo? GetSriLankaTimeZone()
        {
            // Try Windows ID then IANA
            try { return TimeZoneInfo.FindSystemTimeZoneById("Sri Lanka Standard Time"); } catch { }
            try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Colombo"); } catch { }
            return null;
        }

        public static DateTime NowSriLanka()
        {
            var utc = DateTime.UtcNow;
            var tz = GetSriLankaTimeZone();
            if (tz != null)
            {
                var local = TimeZoneInfo.ConvertTimeFromUtc(utc, tz);
                // store as Unspecified to avoid server-local Kind influence but keep the local clock value
                return DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
            }
            // fallback: add offset +5:30
            var fallback = utc.AddHours(5).AddMinutes(30);
            return DateTime.SpecifyKind(fallback, DateTimeKind.Unspecified);
        }

        public static DateTime UtcNow() => DateTime.UtcNow;
    }
}
