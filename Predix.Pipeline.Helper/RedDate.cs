using System;

namespace Predix.Pipeline.Helper
{
    public static class RedDate
    {
        public static DateTime ToUtcDateTimeOrNull(this string epoch)
        {
            if (string.IsNullOrWhiteSpace(epoch))
                return DateTime.MinValue;
            //return DateTimeOffset.FromUnixTimeMilliseconds(Convert.ToInt64(epoch)).DateTime;
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddMilliseconds(Convert.ToInt64(epoch));//.ToLocalTime();
            return dtDateTime;
        }

        public static DateTime ToEst(this DateTime utcDateTime)
        {
            if (utcDateTime.Kind != DateTimeKind.Utc) throw new Exception("dateTime needs to have Kind property set to Utc");
            var toUtcOffset = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time").GetUtcOffset(utcDateTime);
            var convertedTime = DateTime.SpecifyKind(utcDateTime.Add(toUtcOffset), DateTimeKind.Unspecified);
            return new DateTimeOffset(convertedTime, toUtcOffset).DateTime;

            //return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime,
            //    TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"));
        }
    }
}