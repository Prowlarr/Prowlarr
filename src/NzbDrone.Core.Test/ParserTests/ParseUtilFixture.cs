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

        [TestCase(" some  string ", "some string")]
        public void should_normalize_multiple_spaces(string original, string newString)
        {
            ParseUtil.NormalizeMultiSpaces(original).Should().Be(newString);
        }

        [TestCase("1", 1)]
        [TestCase("11", 11)]
        [TestCase("1000 grabs", 1000)]
        [TestCase("2.222", 2222)]
        [TestCase("2,222", 2222)]
        [TestCase("2 222", 2222)]
        [TestCase("2,22", 222)]
        public void should_parse_int_from_string(string original, int parsedInt)
        {
            ParseUtil.CoerceInt(original).Should().Be(parsedInt);
        }

        [TestCase("1.0", 1.0)]
        [TestCase("1.1", 1.1)]
        [TestCase("1000 grabs", 1000.0)]
        [TestCase("2.222", 2.222)]
        [TestCase("2,222", 2.222)]
        [TestCase("2.222,22", 2222.22)]
        [TestCase("2,222.22", 2222.22)]
        [TestCase("2 222", 2222.0)]
        [TestCase("2,22", 2.22)]
        public void should_parse_double_from_string(string original, double parsedInt)
        {
            ParseUtil.CoerceDouble(original).Should().Be(parsedInt);
        }

        [TestCase(null, null)]
        [TestCase("", null)]
        [TestCase("1", 1)]
        [TestCase("1000 grabs", 1000)]
        [TestCase("asdf123asdf", 123)]
        [TestCase("asdf123asdf456asdf", 123)]
        public void should_parse_long_from_string(string original, long? parsedInt)
        {
            ParseUtil.GetLongFromString(original).Should().Be(parsedInt);
        }

        [TestCase("tt0183790", "tt0183790")]
        [TestCase("0183790", "tt0183790")]
        [TestCase("183790", "tt0183790")]
        [TestCase("tt10001870", "tt10001870")]
        [TestCase("10001870", "tt10001870")]
        [TestCase("tt", null)]
        [TestCase("tt0", null)]
        [TestCase("abc", null)]
        [TestCase("abc0", null)]
        [TestCase("0", null)]
        [TestCase("", null)]
        [TestCase(null, null)]
        public void should_parse_full_imdb_id_from_string(string input, string expected)
        {
            ParseUtil.GetFullImdbId(input).Should().Be(expected);
        }
    }
}
