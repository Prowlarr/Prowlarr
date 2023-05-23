//********************************************************************************************
//Author: Sergey Stoyan, CliverSoft.com
//        http://cliversoft.com
//        stoyan@cliversoft.com
//        sergey.stoyan@gmail.com
//        27 February 2007
//********************************************************************************************
using System;
using System.Text.RegularExpressions;

namespace NzbDrone.Core.Parser
{
    public static class DateTimeRoutines
    {
        public class ParsedDateTime
        {
            public readonly int IndexOfDate = -1;
            public readonly int LengthOfDate = -1;
            public readonly int IndexOfTime = -1;
            public readonly int LengthOfTime = -1;
            public readonly DateTime DateTime;
            public readonly bool IsDateFound;
            public readonly bool IsTimeFound;
            public readonly TimeSpan UtcOffset;
            public readonly bool IsUtcOffsetFound;
            public DateTime UtcDateTime;

            internal ParsedDateTime(int index_of_date, int length_of_date, int index_of_time, int length_of_time, DateTime date_time)
            {
                IndexOfDate = index_of_date;
                LengthOfDate = length_of_date;
                IndexOfTime = index_of_time;
                LengthOfTime = length_of_time;
                DateTime = date_time;
                IsDateFound = index_of_date > -1;
                IsTimeFound = index_of_time > -1;
                UtcOffset = new TimeSpan(25, 0, 0);
                IsUtcOffsetFound = false;
                UtcDateTime = new DateTime(1, 1, 1);
            }

            internal ParsedDateTime(int index_of_date, int length_of_date, int index_of_time, int length_of_time, DateTime date_time, TimeSpan utc_offset)
            {
                IndexOfDate = index_of_date;
                LengthOfDate = length_of_date;
                IndexOfTime = index_of_time;
                LengthOfTime = length_of_time;
                DateTime = date_time;
                IsDateFound = index_of_date > -1;
                IsTimeFound = index_of_time > -1;
                UtcOffset = utc_offset;
                IsUtcOffsetFound = Math.Abs(utc_offset.TotalHours) < 12;
                if (!IsUtcOffsetFound)
                {
                    UtcDateTime = new DateTime(1, 1, 1);
                }
                else
                {
                    if (index_of_date < 0)
                    {
                        //to avoid negative date exception when date is undefined
                        var ts = date_time.TimeOfDay + utc_offset;
                        if (ts < new TimeSpan(0))
                        {
                            UtcDateTime = new DateTime(1, 1, 2) + ts;
                        }
                        else
                        {
                            UtcDateTime = new DateTime(1, 1, 1) + ts;
                        }
                    }
                    else
                    {
                        UtcDateTime = date_time + utc_offset;
                    }
                }
            }
        }

        public static DateTime DefaultDate
        {
            get
            {
                if (DefaultDateIsNow)
                {
                    return DateTime.Now;
                }
                else
                {
                    return _DefaultDate;
                }
            }
            set
            {
                _DefaultDate = value;
                DefaultDateIsNow = false;
            }
        }

        private static DateTime _DefaultDate = DateTime.Now;

        public static bool DefaultDateIsNow = true;

        public enum DateTimeFormat
        {
            USDate,
            UKDate,
        }

        public static bool TryParseDateOrTime(this string str, DateTimeFormat default_format, out ParsedDateTime parsed_date_time)
        {
            parsed_date_time = null;

            ParsedDateTime parsed_time;
            if (!TryParseDate(str, default_format, out var parsed_date))
            {
                if (!TryParseTime(str, default_format, out parsed_time, null))
                {
                    return false;
                }

                var date_time = new DateTime(DefaultDate.Year, DefaultDate.Month, DefaultDate.Day, parsed_time.DateTime.Hour, parsed_time.DateTime.Minute, parsed_time.DateTime.Second);
                parsed_date_time = new ParsedDateTime(-1, -1, parsed_time.IndexOfTime, parsed_time.LengthOfTime, date_time, parsed_time.UtcOffset);
            }
            else
            {
                if (!TryParseTime(str, default_format, out parsed_time, parsed_date))
                {
                    var date_time = new DateTime(parsed_date.DateTime.Year, parsed_date.DateTime.Month, parsed_date.DateTime.Day, 0, 0, 0);
                    parsed_date_time = new ParsedDateTime(parsed_date.IndexOfDate, parsed_date.LengthOfDate, -1, -1, date_time);
                }
                else
                {
                    var date_time = new DateTime(parsed_date.DateTime.Year, parsed_date.DateTime.Month, parsed_date.DateTime.Day, parsed_time.DateTime.Hour, parsed_time.DateTime.Minute, parsed_time.DateTime.Second);
                    parsed_date_time = new ParsedDateTime(parsed_date.IndexOfDate, parsed_date.LengthOfDate, parsed_time.IndexOfTime, parsed_time.LengthOfTime, date_time, parsed_time.UtcOffset);
                }
            }

            return true;
        }

        public static bool TryParseTime(this string str, DateTimeFormat default_format, out ParsedDateTime parsed_time, ParsedDateTime parsed_date)
        {
            parsed_time = null;

            string time_zone_r;
            if (default_format == DateTimeFormat.USDate)
            {
                time_zone_r = @"(?:\s*(?'time_zone'UTC|GMT|CST|EST))?";
            }
            else
            {
                time_zone_r = @"(?:\s*(?'time_zone'UTC|GMT))?";
            }

            Match m;
            if (parsed_date != null && parsed_date.IndexOfDate > -1)
            {
                //look around the found date
                //look for <date> hh:mm:ss <UTC offset>
                m = Regex.Match(str.Substring(parsed_date.IndexOfDate + parsed_date.LengthOfDate), @"(?<=^\s*,?\s+|^\s*at\s*|^\s*[T\-]\s*)(?'hour'\d{2})\s*:\s*(?'minute'\d{2})\s*:\s*(?'second'\d{2})\s+(?'offset_sign'[\+\-])(?'offset_hh'\d{2}):?(?'offset_mm'\d{2})(?=$|[^\d\w])", RegexOptions.Compiled);
                if (!m.Success)
                {
                    //look for <date> [h]h:mm[:ss] [PM/AM] [UTC/GMT]
                    m = Regex.Match(str.Substring(parsed_date.IndexOfDate + parsed_date.LengthOfDate), @"(?<=^\s*,?\s+|^\s*at\s*|^\s*[T\-]\s*)(?'hour'\d{1,2})\s*:\s*(?'minute'\d{2})\s*(?::\s*(?'second'\d{2}))?(?:\s*(?'ampm'AM|am|PM|pm))?" + time_zone_r + @"(?=$|[^\d\w])", RegexOptions.Compiled);
                }

                if (!m.Success)
                {
                    //look for [h]h:mm:ss [PM/AM] [UTC/GMT] <date>
                    m = Regex.Match(str.Substring(0, parsed_date.IndexOfDate), @"(?<=^|[^\d])(?'hour'\d{1,2})\s*:\s*(?'minute'\d{2})\s*(?::\s*(?'second'\d{2}))?(?:\s*(?'ampm'AM|am|PM|pm))?" + time_zone_r + @"(?=$|[\s,]+)", RegexOptions.Compiled);
                }

                if (!m.Success)
                {
                    //look for [h]h:mm:ss [PM/AM] [UTC/GMT] within <date>
                    m = Regex.Match(str.Substring(parsed_date.IndexOfDate, parsed_date.LengthOfDate), @"(?<=^|[^\d])(?'hour'\d{1,2})\s*:\s*(?'minute'\d{2})\s*(?::\s*(?'second'\d{2}))?(?:\s*(?'ampm'AM|am|PM|pm))?" + time_zone_r + @"(?=$|[\s,]+)", RegexOptions.Compiled);
                }
            }
            else
            {
                //look anywhere within string
                //look for hh:mm:ss <UTC offset>
                m = Regex.Match(str, @"(?<=^|\s+|\s*T\s*)(?'hour'\d{2})\s*:\s*(?'minute'\d{2})\s*:\s*(?'second'\d{2})\s+(?'offset_sign'[\+\-])(?'offset_hh'\d{2}):?(?'offset_mm'\d{2})?(?=$|[^\d\w])", RegexOptions.Compiled);
                if (!m.Success)
                {
                    //look for [h]h:mm[:ss] [PM/AM] [UTC/GMT]
                    m = Regex.Match(str, @"(?<=^|\s+|\s*T\s*)(?'hour'\d{1,2})\s*:\s*(?'minute'\d{2})\s*(?::\s*(?'second'\d{2}))?(?:\s*(?'ampm'AM|am|PM|pm))?" + time_zone_r + @"(?=$|[^\d\w])", RegexOptions.Compiled);
                }
            }

            if (!m.Success)
            {
                return false;
            }

            //try
            //{
            var hour = int.Parse(m.Groups["hour"].Value);
            if (hour < 0 || hour > 23)
            {
                return false;
            }

            var minute = int.Parse(m.Groups["minute"].Value);
            if (minute < 0 || minute > 59)
            {
                return false;
            }

            var second = 0;
            if (!string.IsNullOrEmpty(m.Groups["second"].Value))
            {
                second = int.Parse(m.Groups["second"].Value);
                if (second < 0 || second > 59)
                {
                    return false;
                }
            }

            if (string.Equals(m.Groups["ampm"].Value, "PM", StringComparison.InvariantCultureIgnoreCase) && hour < 12)
            {
                hour += 12;
            }
            else if (string.Equals(m.Groups["ampm"].Value, "AM", StringComparison.InvariantCultureIgnoreCase) && hour == 12)
            {
                hour -= 12;
            }

            var date_time = new DateTime(1, 1, 1, hour, minute, second);

            if (m.Groups["offset_hh"].Success)
            {
                var offset_hh = int.Parse(m.Groups["offset_hh"].Value);
                var offset_mm = 0;
                if (m.Groups["offset_mm"].Success)
                {
                    offset_mm = int.Parse(m.Groups["offset_mm"].Value);
                }

                var utc_offset = new TimeSpan(offset_hh, offset_mm, 0);
                if (m.Groups["offset_sign"].Value == "-")
                {
                    utc_offset = -utc_offset;
                }

                parsed_time = new ParsedDateTime(-1, -1, m.Index, m.Length, date_time, utc_offset);
                return true;
            }

            if (m.Groups["time_zone"].Success)
            {
                TimeSpan utc_offset;
                switch (m.Groups["time_zone"].Value)
                {
                    case "UTC":
                    case "GMT":
                        utc_offset = new TimeSpan(0, 0, 0);
                        break;
                    case "CST":
                        utc_offset = new TimeSpan(-6, 0, 0);
                        break;
                    case "EST":
                        utc_offset = new TimeSpan(-5, 0, 0);
                        break;
                    default:
                        throw new Exception("Time zone: " + m.Groups["time_zone"].Value + " is not defined.");
                }

                parsed_time = new ParsedDateTime(-1, -1, m.Index, m.Length, date_time, utc_offset);
                return true;
            }

            parsed_time = new ParsedDateTime(-1, -1, m.Index, m.Length, date_time);

            return true;
        }

        public static bool TryParseDate(this string str, DateTimeFormat default_format, out ParsedDateTime parsed_date)
        {
            parsed_date = null;

            if (string.IsNullOrEmpty(str))
            {
                return false;
            }

            //look for dd/mm/yy
            var m = Regex.Match(str, @"(?<=^|[^\d])(?'day'\d{1,2})\s*(?'separator'[\\/\.])+\s*(?'month'\d{1,2})\s*\'separator'+\s*(?'year'\d{2}|\d{4})(?=$|[^\d])", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            if (m.Success)
            {
                DateTime date;
                if ((default_format ^ DateTimeFormat.USDate) == DateTimeFormat.USDate)
                {
                    if (!ConvertToDate(int.Parse(m.Groups["year"].Value), int.Parse(m.Groups["day"].Value), int.Parse(m.Groups["month"].Value), out date))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!ConvertToDate(int.Parse(m.Groups["year"].Value), int.Parse(m.Groups["month"].Value), int.Parse(m.Groups["day"].Value), out date))
                    {
                        return false;
                    }
                }

                parsed_date = new ParsedDateTime(m.Index, m.Length, -1, -1, date);
                return true;
            }

            //look for [yy]yy-mm-dd
            m = Regex.Match(str, @"(?<=^|[^\d])(?'year'\d{2}|\d{4})\s*(?'separator'[\-])\s*(?'month'\d{1,2})\s*\'separator'+\s*(?'day'\d{1,2})(?=$|[^\d])", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            if (m.Success)
            {
                if (!ConvertToDate(int.Parse(m.Groups["year"].Value), int.Parse(m.Groups["month"].Value), int.Parse(m.Groups["day"].Value), out var date))
                {
                    return false;
                }

                parsed_date = new ParsedDateTime(m.Index, m.Length, -1, -1, date);
                return true;
            }

            //look for month dd yyyy
            m = Regex.Match(str, @"(?:^|[^\d\w])(?'month'Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)[uarychilestmbro]*\s+(?'day'\d{1,2})(?:-?st|-?th|-?rd|-?nd)?\s*,?\s*(?'year'\d{4})(?=$|[^\d\w])", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            if (!m.Success)
            {
                //look for dd month [yy]yy
                m = Regex.Match(str, @"(?:^|[^\d\w:])(?'day'\d{1,2})(?:-?st\s+|-?th\s+|-?rd\s+|-?nd\s+|-|\s+)(?'month'Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)[uarychilestmbro]*(?:\s*,?\s*|-)'?(?'year'\d{2}|\d{4})(?=$|[^\d\w])", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            }

            if (!m.Success)
            {
                //look for yyyy month dd
                m = Regex.Match(str, @"(?:^|[^\d\w])(?'year'\d{4})\s+(?'month'Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)[uarychilestmbro]*\s+(?'day'\d{1,2})(?:-?st|-?th|-?rd|-?nd)?(?=$|[^\d\w])", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            }

            if (!m.Success)
            {
                //look for month dd hh:mm:ss MDT|UTC yyyy
                m = Regex.Match(str, @"(?:^|[^\d\w])(?'month'Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)[uarychilestmbro]*\s+(?'day'\d{1,2})\s+\d{2}\:\d{2}\:\d{2}\s+(?:MDT|UTC)\s+(?'year'\d{4})(?=$|[^\d\w])", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            }

            if (!m.Success)
            {
                //look for  month dd [yyyy]
                m = Regex.Match(str, @"(?:^|[^\d\w])(?'month'Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)[uarychilestmbro]*\s+(?'day'\d{1,2})(?:-?st|-?th|-?rd|-?nd)?(?:\s*,?\s*(?'year'\d{4}))?(?=$|[^\d\w])", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            }

            if (m.Success)
            {
                var month = -1;
                var index_of_date = m.Index;
                var length_of_date = m.Length;

                switch (m.Groups["month"].Value)
                {
                    case "Jan":
                    case "JAN":
                        month = 1;
                        break;
                    case "Feb":
                    case "FEB":
                        month = 2;
                        break;
                    case "Mar":
                    case "MAR":
                        month = 3;
                        break;
                    case "Apr":
                    case "APR":
                        month = 4;
                        break;
                    case "May":
                    case "MAY":
                        month = 5;
                        break;
                    case "Jun":
                    case "JUN":
                        month = 6;
                        break;
                    case "Jul":
                        month = 7;
                        break;
                    case "Aug":
                    case "AUG":
                        month = 8;
                        break;
                    case "Sep":
                    case "SEP":
                        month = 9;
                        break;
                    case "Oct":
                    case "OCT":
                        month = 10;
                        break;
                    case "Nov":
                    case "NOV":
                        month = 11;
                        break;
                    case "Dec":
                    case "DEC":
                        month = 12;
                        break;
                }

                int year;
                if (!string.IsNullOrEmpty(m.Groups["year"].Value))
                {
                    year = int.Parse(m.Groups["year"].Value);
                }
                else
                {
                    year = DefaultDate.Year;
                }

                if (!ConvertToDate(year, month, int.Parse(m.Groups["day"].Value), out var date))
                {
                    return false;
                }

                parsed_date = new ParsedDateTime(index_of_date, length_of_date, -1, -1, date);
                return true;
            }

            return false;
        }

        private static bool ConvertToDate(int year, int month, int day, out DateTime date)
        {
            if (year >= 100)
            {
                if (year < 1000)
                {
                    date = new DateTime(1, 1, 1);
                    return false;
                }
            }
            else
                if (year > 30)
            {
                year += 1900;
            }
            else
            {
                year += 2000;
            }

            try
            {
                date = new DateTime(year, month, day);
            }
            catch
            {
                date = new DateTime(1, 1, 1);
                return false;
            }

            return true;
        }
    }
}
