using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.ParserTests
{
    [TestFixture]
    public class ParseUtilFixture : CoreTest
    {
        [TestCase("1023.4 KB", 1047961)]
        [TestCase("1023.4 MB", 1073112704)]
        [TestCase("1,023.4 MB", 1073112704)]
        [TestCase("1.023,4 MB", 1073112704)]
        [TestCase("1 023,4 MB", 1073112704)]
        [TestCase("1.023.4 MB", 1073112704)]
        [TestCase("1023.4 GB", 1098867408896)]
        [TestCase("1023.4 TB", 1125240226709504)]
        public void should_parse_size(string stringSize, long size)
        {
            ParseUtil.GetBytes(stringSize).Should().Be(size);
        }
    }
}
