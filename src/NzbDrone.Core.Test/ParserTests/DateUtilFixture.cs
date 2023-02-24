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
    public class DateUtilFixture : CoreTest
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
    }
}
