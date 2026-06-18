using System;
using System.Globalization;
using System.Threading;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.ParserTests
{
    [TestFixture]
    public class DateTimeUtilFixture : CoreTest
    {
        [TestCase("pt-BR")]
        [TestCase("en-US")]
        public void should_format_date_invariant(string culture)
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo(culture);

            var dateNow = DateTime.Now;

            DateTimeUtil.FromUnknown(dateNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture))
                .ToString(DateTimeUtil.Rfc1123ZPattern, CultureInfo.InvariantCulture)
                .Should().Be(dateNow.ToString("ddd, dd MMM yyyy HH':'mm':'ss z", CultureInfo.InvariantCulture));
        }

        [TestCase("2022-08-08 02:07:39 -02:00", "2006-01-02 15:04:05 -07:00", "yyyy-MM-dd HH:mm:ss zzz", "2022-08-08 04:07:39 +00:00")]
        [TestCase("2022-08-08 02:07:39 -02:00", "yyyy-MM-dd HH:mm:ss zzz", "yyyy-MM-dd HH:mm:ss zzz", "2022-08-08 04:07:39 +00:00")]
        [TestCase("2022-08-08 -02:00", "2006-01-02 -07:00", "yyyy-MM-dd zzz", "2022-08-08 +00:00")]
        [TestCase("2022-08-08 -02:00", "yyyy-MM-dd zzz", "yyyy-MM-dd zzz", "2022-08-08 +00:00")]
        [TestCase("02:07:39 -02:00", "15:04:05 -07:00", "HH:mm:ss zzz", "04:07:39 +00:00")]
        [TestCase("02:07:39 -02:00", "HH:mm:ss zzz", "HH:mm:ss zzz", "04:07:39 +00:00")]
        [TestCase("-02:00", "zzz", "zzz", "+00:00")]
        [TestCase("-02:00", "-07:00", "zzz", "+00:00")]
        public void parse_datetime_golang(string dateInput, string format, string standardFormat, string expectedDate)
        {
            DateTimeUtil.ParseDateTimeGoLang(dateInput, format)
                .ToUniversalTime()
                .ToString(standardFormat, CultureInfo.InvariantCulture)
                .Should().Be(expectedDate);
        }

        [TestCase("2026-06-03 01:01:21", "yyyy-MM-dd HH:mm:ss")]
        [TestCase("2022-08-08 02:07:39", "yyyy-MM-dd HH:mm:ss")]
        [TestCase("2022-01-15 12:30:45", "yyyy-MM-dd HH:mm:ss")]
        public void parse_datetime_golang_timezone_less_should_assume_utc(string dateInput, string format)
        {
            var result = DateTimeUtil.ParseDateTimeGoLang(dateInput, format);

            result.Kind.Should().Be(DateTimeKind.Utc);
            result.Year.Should().Be(int.Parse(dateInput.AsSpan(0, 4)));
            result.Month.Should().Be(int.Parse(dateInput.AsSpan(5, 2)));
            result.Day.Should().Be(int.Parse(dateInput.AsSpan(8, 2)));
        }

        [TestCase("2026-06-03 01:01:21", "2006-01-02 15:04:05")]
        [TestCase("2022-08-08 02:07:39", "2006-01-02 15:04:05")]
        public void parse_datetime_golang_golang_format_timezone_less_should_assume_utc(string dateInput, string format)
        {
            var result = DateTimeUtil.ParseDateTimeGoLang(dateInput, format);

            result.Kind.Should().Be(DateTimeKind.Utc);
        }

        [TestCase("2022-08-08 02:07:39 -02:00", "yyyy-MM-dd HH:mm:ss zzz")]
        [TestCase("2022-08-08 02:07:39 -02:00", "2006-01-02 15:04:05 -07:00")]
        public void parse_datetime_golang_timezone_aware_should_not_assume_utc(string dateInput, string format)
        {
            var result = DateTimeUtil.ParseDateTimeGoLang(dateInput, format);

            result.Kind.Should().NotBe(DateTimeKind.Unspecified);
        }
    }
}
