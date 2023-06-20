using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.Parser
{
    public static class DateTimeUtil
    {
        public const string Rfc1123ZPattern = "ddd, dd MMM yyyy HH':'mm':'ss z";

        private static readonly Regex _TimeAgoRegexp = new Regex(@"(?i)\bago", RegexOptions.Compiled);
        private static readonly Regex _TodayRegexp = new Regex(@"(?i)\btoday(?:[\s,]+(?:at){0,1}\s*|[\s,]*|$)", RegexOptions.Compiled);
        private static readonly Regex _TomorrowRegexp = new Regex(@"(?i)\btomorrow(?:[\s,]+(?:at){0,1}\s*|[\s,]*|$)", RegexOptions.Compiled);
        private static readonly Regex _YesterdayRegexp = new Regex(@"(?i)\byesterday(?:[\s,]+(?:at){0,1}\s*|[\s,]*|$)", RegexOptions.Compiled);
        private static readonly Regex _DaysOfWeekRegexp = new Regex(@"(?i)\b(monday|tuesday|wednesday|thursday|friday|saturday|sunday)\s+at\s+", RegexOptions.Compiled);
        private static readonly Regex _MissingYearRegexp = new Regex(@"^(\d{1,2}-\d{1,2})(\s|$)", RegexOptions.Compiled);
        private static readonly Regex _MissingYearRegexp2 = new Regex(@"^(\d{1,2}\s+\w{3})\s+(\d{1,2}\:\d{1,2}.*)$", RegexOptions.Compiled); // 1 Jan 10:30

        public static DateTime UnixTimestampToDateTime(long unixTime)
        {
            var dt = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dt = dt.AddSeconds(unixTime).ToLocalTime();
            return dt;
        }

        public static DateTime UnixTimestampToDateTime(double unixTime)
        {
            var unixStart = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            var unixTimeStampInTicks = (long)(unixTime * TimeSpan.TicksPerSecond);
            return new DateTime(unixStart.Ticks + unixTimeStampInTicks);
        }

        public static double DateTimeToUnixTimestamp(DateTime dt)
        {
            var date = dt.ToUniversalTime();
            var ticks = date.Ticks - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).Ticks;
            var ts = ticks / TimeSpan.TicksPerSecond;
            return ts;
        }

        // ex: "2 hours 1 day"
        public static DateTime FromTimeAgo(string str)
        {
            str = str.ToLowerInvariant();
            if (str.Contains("now"))
            {
                return DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Local);
            }

            str = str.Replace(",", "");
            str = str.Replace("ago", "");
            str = str.Replace("and", "");

            var timeAgo = TimeSpan.Zero;
            var timeagoRegex = new Regex(@"\s*?([\d\.]+)\s*?([^\d\s\.]+)\s*?");
            var timeagoMatches = timeagoRegex.Match(str);

            while (timeagoMatches.Success)
            {
                var val = ParseUtil.CoerceFloat(timeagoMatches.Groups[1].Value);
                var unit = timeagoMatches.Groups[2].Value;
                timeagoMatches = timeagoMatches.NextMatch();

                if (unit.Contains("sec") || unit == "s")
                {
                    timeAgo += TimeSpan.FromSeconds(val);
                }
                else if (unit.Contains("min") || unit == "m")
                {
                    timeAgo += TimeSpan.FromMinutes(val);
                }
                else if (unit.Contains("hour") || unit.Contains("hr") || unit == "h")
                {
                    timeAgo += TimeSpan.FromHours(val);
                }
                else if (unit.Contains("day") || unit == "d")
                {
                    timeAgo += TimeSpan.FromDays(val);
                }
                else if (unit.Contains("week") || unit.Contains("wk") || unit == "w")
                {
                    timeAgo += TimeSpan.FromDays(val * 7);
                }
                else if (unit.Contains("month") || unit == "mo")
                {
                    timeAgo += TimeSpan.FromDays(val * 30);
                }
                else if (unit.Contains("year") || unit == "y")
                {
                    timeAgo += TimeSpan.FromDays(val * 365);
                }
                else
                {
                    throw new InvalidDateException("TimeAgo parsing failed, unknown unit: " + unit);
                }
            }

            return DateTime.SpecifyKind(DateTime.Now - timeAgo, DateTimeKind.Local);
        }

        // Uses the DateTimeRoutines library to parse the date
        // http://www.codeproject.com/Articles/33298/C-Date-Time-Parser
        public static DateTime FromFuzzyTime(string str, string format = null)
        {
            var dtFormat = format == "UK" ?
                DateTimeRoutines.DateTimeFormat.UKDate :
                DateTimeRoutines.DateTimeFormat.USDate;

            if (DateTimeRoutines.TryParseDateOrTime(
                str, dtFormat, out var dt))
            {
                return dt.DateTime;
            }

            throw new InvalidDateException($"FromFuzzyTime parsing failed for string {str}");
        }

        public static DateTime FromUnknown(string str, string format = null)
        {
            try
            {
                str = str.Trim();

                if (DateTime.TryParseExact(str, Rfc1123ZPattern, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
                {
                    return parsedDate;
                }

                // try parsing the str as an unix timestamp
                if (str.IsAllDigits() && long.TryParse(str, out var unixTimeStamp))
                {
                    return UnixTimestampToDateTime(unixTimeStamp);
                }

                if (str.ToLower().Contains("now"))
                {
                    return DateTime.UtcNow;
                }

                // ... ago
                var match = _TimeAgoRegexp.Match(str);
                if (match.Success)
                {
                    var timeago = str;
                    return FromTimeAgo(timeago);
                }

                // Today ...
                match = _TodayRegexp.Match(str);
                if (match.Success)
                {
                    var time = str.Replace(match.Groups[0].Value, "");
                    var dt = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Unspecified);
                    dt += ParseTimeSpan(time);
                    return dt;
                }

                // Yesterday ...
                match = _YesterdayRegexp.Match(str);
                if (match.Success)
                {
                    var time = str.Replace(match.Groups[0].Value, "");
                    var dt = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Unspecified);
                    dt += ParseTimeSpan(time);
                    dt -= TimeSpan.FromDays(1);
                    return dt;
                }

                // Tomorrow ...
                match = _TomorrowRegexp.Match(str);
                if (match.Success)
                {
                    var time = str.Replace(match.Groups[0].Value, "");
                    var dt = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Unspecified);
                    dt += ParseTimeSpan(time);
                    dt += TimeSpan.FromDays(1);
                    return dt;
                }

                // [day of the week] at ... (eg: Saturday at 14:22)
                match = _DaysOfWeekRegexp.Match(str);
                if (match.Success)
                {
                    var time = str.Replace(match.Groups[0].Value, "");
                    var dt = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Unspecified);
                    dt += ParseTimeSpan(time);

                    DayOfWeek dow;
                    var groupMatchLower = match.Groups[1].Value.ToLower();
                    if (groupMatchLower.StartsWith("monday"))
                    {
                        dow = DayOfWeek.Monday;
                    }
                    else if (groupMatchLower.StartsWith("tuesday"))
                    {
                        dow = DayOfWeek.Tuesday;
                    }
                    else if (groupMatchLower.StartsWith("wednesday"))
                    {
                        dow = DayOfWeek.Wednesday;
                    }
                    else if (groupMatchLower.StartsWith("thursday"))
                    {
                        dow = DayOfWeek.Thursday;
                    }
                    else if (groupMatchLower.StartsWith("friday"))
                    {
                        dow = DayOfWeek.Friday;
                    }
                    else if (groupMatchLower.StartsWith("saturday"))
                    {
                        dow = DayOfWeek.Saturday;
                    }
                    else
                    {
                        dow = DayOfWeek.Sunday;
                    }

                    while (dt.DayOfWeek != dow)
                    {
                        dt = dt.AddDays(-1);
                    }

                    return dt;
                }

                // add missing year
                match = _MissingYearRegexp.Match(str);
                if (match.Success)
                {
                    var date = match.Groups[1].Value;
                    var newDate = DateTime.Now.Year + "-" + date;
                    str = str.Replace(date, newDate);
                }

                // add missing year 2
                match = _MissingYearRegexp2.Match(str);
                if (match.Success)
                {
                    var date = match.Groups[1].Value;
                    var time = match.Groups[2].Value;
                    str = date + " " + DateTime.Now.Year + " " + time;
                }

                return FromFuzzyTime(str, format);
            }
            catch (Exception ex)
            {
                throw new InvalidDateException($"DateTime parsing failed for \"{str}\": {ex}");
            }
        }

        // converts a date/time string to a DateTime object using a GoLang layout
        public static DateTime ParseDateTimeGoLang(string date, string layout)
        {
            date = date.Trim();

            var commonStandardFormats = new[] { "y", "h", "d" };

            if (commonStandardFormats.Any(layout.ContainsIgnoreCase) && DateTime.TryParseExact(date, layout, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
            {
                return parsedDate;
            }

            var format = layout

                // year
                .Replace("2006", "yyyy")
                .Replace("06", "yy")

                // month
                .Replace("January", "MMMM")
                .Replace("Jan", "MMM")
                .Replace("01", "MM")

                // day
                .Replace("Monday", "dddd")
                .Replace("Mon", "ddd")
                .Replace("02", "dd")
                .Replace("2", "d")

                // hours/minutes/seconds
                .Replace("05", "ss")
                .Replace("15", "HH")
                .Replace("03", "hh")
                .Replace("3", "h")
                .Replace("04", "mm")
                .Replace("4", "m")
                .Replace("5", "s")

                // month again
                .Replace("1", "M")

                // fractional seconds
                .Replace(".0000", "ffff")
                .Replace(".000", "fff")
                .Replace(".00", "ff")
                .Replace(".0", "f")
                .Replace(".9999", "FFFF")
                .Replace(".999", "FFF")
                .Replace(".99", "FF")
                .Replace(".9", "F")

                // AM/PM
                .Replace("PM", "tt")
                .Replace("pm", "tt") // not sure if this works

                // timezones
                // these might need further tuning
                .Replace("Z07:00", "'Z'zzz")
                .Replace("Z07", "'Z'zz")
                .Replace("Z07:00", "'Z'zzz")
                .Replace("Z07", "'Z'zz")
                .Replace("-07:00", "zzz")
                .Replace("-07", "zz");

            try
            {
                return DateTime.ParseExact(date, format, CultureInfo.InvariantCulture);
            }
            catch (FormatException ex)
            {
                throw new InvalidDateException($"Error while parsing DateTime \"{date}\", using layout \"{layout}\" ({format}): {ex.Message}", ex);
            }
        }

        private static TimeSpan ParseTimeSpan(string time) =>
            string.IsNullOrWhiteSpace(time)
                ? TimeSpan.Zero
                : DateTime.Parse(time).TimeOfDay;
    }
}
