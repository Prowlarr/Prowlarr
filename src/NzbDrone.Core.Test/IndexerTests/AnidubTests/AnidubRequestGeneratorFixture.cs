using System.Linq;
using System.Text;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Indexers.Definitions;
using NzbDrone.Core.Indexers.Settings;
using NzbDrone.Core.IndexerSearch.Definitions;

namespace NzbDrone.Core.Test.IndexerTests.AnidubTests
{
    [TestFixture]
    public class AnidubRequestGeneratorFixture
    {
        private AnidubRequestGenerator _generator;

        [SetUp]
        public void Setup()
        {
            _generator = new AnidubRequestGenerator(new UserPassTorrentBaseSettings { BaseUrl = "https://tr.anidub.com/" });
        }

        [TestCase("Sousou no Frieren", "story=Frieren")]
        [TestCase("Spy x Family", "story=Family")]
        [TestCase("Sousou no Frieren S02", "story=Frieren")]
        [TestCase("Dandadan", "story=Dandadan")]
        [TestCase("Frieren S01", "story=Frieren")]
        public void should_search_by_longest_word_of_term(string term, string expectedStoryParameter)
        {
            var requests = _generator.GetSearchRequests(new BasicSearchCriteria { SearchTerm = term })
                .GetAllTiers()
                .SelectMany(tier => tier)
                .ToList();

            requests.Should().HaveCount(1);

            var body = Encoding.UTF8.GetString(requests.Single().HttpRequest.ContentData);

            body.Should().Contain(expectedStoryParameter);
        }
    }
}
