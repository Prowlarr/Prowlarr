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
    }
}
