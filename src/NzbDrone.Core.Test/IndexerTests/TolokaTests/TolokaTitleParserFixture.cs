using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Indexers.Definitions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Test.IndexerTests.TolokaTests
{
    [TestFixture]
    public class TolokaTitleParserFixture
    {
        private TolokaTitleParser _parser;

        [SetUp]
        public void Setup()
        {
            _parser = new TolokaTitleParser();
        }

        [TestCase("Castlevania S03E01 1080p WEB-DL", false, "Castlevania S03E01 1080p WEB-DL")]
        [TestCase("Castlevania S03E01 1080p WEB-DL", true, "Castlevania S03E01 1080p WEB-DL UKR")]
        [TestCase("Castlevania S03E01 1080p WEB-DL UKR", true, "Castlevania S03E01 1080p WEB-DL UKR")]
        public void should_parse_title_with_ukr_option(string title, bool addUkrainianToTitle, string expected)
        {
            var result = _parser.Parse(title, new List<IndexerCategory>(), addUkrainianToTitle: addUkrainianToTitle);

            result.Should().Be(expected);
        }
    }
}
