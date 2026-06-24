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

        [Test]
        public void should_not_append_ukr_when_disabled()
        {
            var result = _parser.Parse("Castlevania S03E01 1080p WEB-DL", new List<IndexerCategory>(), addUkrainianToTitle: false);

            result.Should().NotEndWith("UKR");
        }

        [Test]
        public void should_append_ukr_when_enabled()
        {
            var result = _parser.Parse("Castlevania S03E01 1080p WEB-DL", new List<IndexerCategory>(), addUkrainianToTitle: true);

            result.Should().EndWith("UKR");
        }

        [Test]
        public void should_append_ukr_after_cyrillic_stripping()
        {
            var result = _parser.Parse("Замок Кастлеванія / Castlevania S03E01 1080p WEB-DL", new List<IndexerCategory>(), stripCyrillicLetters: true, addUkrainianToTitle: true);

            result.Should().EndWith("UKR");
            result.Should().Contain("Castlevania");
        }
    }
}
