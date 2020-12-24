using System.Linq;
using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using NzbDrone.Common.Http;
using NzbDrone.Core.Indexers.HDBits;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IndexerTests.HDBitsTests
{
    public class HDBitsRequestGeneratorFixture : CoreTest<HDBitsRequestGenerator>
    {
        private MovieSearchCriteria _movieSearchCriteria;

        [SetUp]
        public void Setup()
        {
            Subject.Settings = new HDBitsSettings()
            {
                BaseUrl = "http://127.0.0.1:1234/",
                ApiKey = "abcd",
                Username = "somename"
            };

            _movieSearchCriteria = new MovieSearchCriteria
            {
                Categories = new int[] { 2000, 2010 },
                ImdbId = "tt0076759"
            };
        }

        private void MovieWithoutIMDB()
        {
            _movieSearchCriteria.ImdbId = null;
        }

        [Test]
        public void should_search_by_imdbid_if_supported()
        {
            var results = Subject.GetSearchRequests(_movieSearchCriteria);
            var imdbQuery = int.Parse(_movieSearchCriteria.ImdbId.Substring(2));

            results.GetAllTiers().Should().HaveCount(1);

            var page = results.GetAllTiers().First().First();

            var encoding = HttpHeader.GetEncodingFromContentType(page.HttpRequest.Headers.ContentType);

            var body = encoding.GetString(page.HttpRequest.ContentData);
            var query = JsonConvert.DeserializeObject<TorrentQuery>(body);

            query.Category.Should().HaveCount(2);
            query.ImdbInfo.Id.Should().Be(imdbQuery);
        }

        [Test]
        public void should_return_no_results_if_no_imdb()
        {
            MovieWithoutIMDB();

            var results = Subject.GetSearchRequests(_movieSearchCriteria);
            results.GetTier(0).Should().HaveCount(0);
        }
    }
}
