using System;
using System.Globalization;
using BenchmarkDotNet.Attributes;
using NzbDrone.Core.Parser;

namespace NzbDrone.Benchmark.Test.ParserTests
{
    [InProcess]
    public class DateTimeUtilFixture
    {
        [Benchmark]
        [Arguments("2022-08-08 02:07:39")]
        public void parse_fuzzy_date(string dateInput)
        {
            DateTimeUtil.FromFuzzyTime(dateInput);
        }

        [Benchmark]
        [Arguments("18 hours ago")]
        public void parse_time_ago(string dateInput)
        {
            DateTimeUtil.FromTimeAgo(dateInput);
        }

        [Benchmark]
        [Arguments("2022-08-08 02:07:39")]
        [Arguments("1677173189")]
        public void parse_unknown_date(string dateInput)
        {
            DateTimeUtil.FromUnknown(dateInput);
        }

        [Benchmark]
        [Arguments("2022-08-08 02:07:39 -02:00", "2006-01-02 15:04:05 -07:00")]
        [Arguments("2022-08-08 02:07:39 -02:00", "yyyy-MM-dd HH:mm:ss zzz")]
        public void parse_datetime_golang(string dateInput, string format)
        {
            DateTimeUtil.ParseDateTimeGoLang(dateInput, format);
        }

        [Benchmark]
        [Arguments("2022-08-08 02:07:39 -02:00", "yyyy-MM-dd HH:mm:ss zzz")]
        public void parse_datetime_exact(string dateInput, string format)
        {
            DateTime.ParseExact(dateInput, format, CultureInfo.InvariantCulture);
        }
    }
}
