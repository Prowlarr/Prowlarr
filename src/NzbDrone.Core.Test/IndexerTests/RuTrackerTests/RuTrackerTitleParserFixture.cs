using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Indexers.Definitions;

namespace NzbDrone.Core.Test.IndexerTests.RuTrackerTests
{
    [TestFixture]
    public class RuTrackerTitleParserFixture
    {
        private static readonly ICollection<IndexerCategory> TvCategories = new List<IndexerCategory> { NewznabStandardCategory.TVHD };

        private readonly RuTrackerTitleParser _titleParser = new();

        [TestCase("Series Title (2019) WEB-DL 1080p", "Series Title (2019) WEB-DL 1080p RUS")]
        [TestCase("Series Title (2019) WEB-DL 1080p RUS", "Series Title (2019) WEB-DL 1080p RUS")]
        public void should_add_rus_to_title_without_duplicating(string title, string expected)
        {
            _titleParser.Parse(title, TvCategories, stripCyrillicLetters: false, addRussianToTitle: true)
                .Should().Be(expected);
        }
    }
}
