using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Indexers.Newznab;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IndexerTests.NewznabTests
{
    public class NewznabRequestGeneratorFixture : CoreTest<NewznabRequestGenerator>
    {
        private MovieSearchCriteria _movieSearchCriteria;
        private IndexerCapabilities _capabilities;

        [SetUp]
        public void SetUp()
        {
            Subject.Settings = new NewznabSettings()
            {
                BaseUrl = "http://127.0.0.1:1234/",
                ApiKey = "abcd",
            };

            _movieSearchCriteria = new MovieSearchCriteria
            {
                SearchTerm = "Star Wars"
            };

            _capabilities = new IndexerCapabilities();

            Mocker.GetMock<INewznabCapabilitiesProvider>()
                .Setup(v => v.GetCapabilities(It.IsAny<NewznabSettings>()))
                .Returns(_capabilities);
        }

        [Test]
        public void should_return_subsequent_pages()
        {
            var results = Subject.GetSearchRequests(_movieSearchCriteria);

            results.GetAllTiers().Should().HaveCount(2);

            var pages = results.GetAllTiers().First().Take(3).ToList();

            pages[0].Url.FullUri.Should().Contain("&offset=0&");
            pages[1].Url.FullUri.Should().Contain("&offset=100&");
            pages[2].Url.FullUri.Should().Contain("&offset=200&");
        }

        [Test]
        public void should_not_get_unlimited_pages()
        {
            var results = Subject.GetSearchRequests(_movieSearchCriteria);

            results.GetAllTiers().Should().HaveCount(2);

            var pages = results.GetAllTiers().First().Take(500).ToList();

            pages.Count.Should().BeLessThan(500);
        }

        [Test]
        public void should_not_search_by_imdbid_if_not_supported()
        {
            _capabilities.MovieSearchParams = new List<MovieSearchParam> { MovieSearchParam.Q };

            var results = Subject.GetSearchRequests(_movieSearchCriteria);

            results.GetAllTiers().Should().HaveCount(1);

            var page = results.GetAllTiers().First().First();

            page.Url.Query.Should().NotContain("imdbid=0076759");
            page.Url.Query.Should().Contain("q=Star");
        }

        [Test]
        public void should_search_by_imdbid_if_supported()
        {
            _capabilities.MovieSearchParams = new List<MovieSearchParam> { MovieSearchParam.Q, MovieSearchParam.ImdbId };

            var results = Subject.GetSearchRequests(_movieSearchCriteria);
            results.GetTier(0).Should().HaveCount(1);

            var page = results.GetAllTiers().First().First();

            page.Url.Query.Should().Contain("imdbid=0076759");
        }

        [Test]
        public void should_search_by_tmdbid_if_supported()
        {
            _capabilities.MovieSearchParams = new List<MovieSearchParam> { MovieSearchParam.Q, MovieSearchParam.TmdbId };

            var results = Subject.GetSearchRequests(_movieSearchCriteria);
            results.GetTier(0).Should().HaveCount(1);

            var page = results.GetAllTiers().First().First();

            page.Url.Query.Should().Contain("tmdbid=11");
        }

        [Test]
        public void should_prefer_search_by_tmdbid_if_rid_supported()
        {
            _capabilities.MovieSearchParams = new List<MovieSearchParam> { MovieSearchParam.Q, MovieSearchParam.ImdbId, MovieSearchParam.TmdbId };

            var results = Subject.GetSearchRequests(_movieSearchCriteria);
            results.GetTier(0).Should().HaveCount(1);

            var page = results.GetAllTiers().First().First();

            page.Url.Query.Should().Contain("tmdbid=11");
            page.Url.Query.Should().NotContain("imdbid=0076759");
        }

        [Test]
        public void should_use_aggregrated_id_search_if_supported()
        {
            _capabilities.MovieSearchParams = new List<MovieSearchParam> { MovieSearchParam.Q, MovieSearchParam.ImdbId, MovieSearchParam.TmdbId };

            var results = Subject.GetSearchRequests(_movieSearchCriteria);
            results.GetTier(0).Should().HaveCount(1);

            var page = results.GetTier(0).First().First();

            page.Url.Query.Should().Contain("tmdbid=11");
            page.Url.Query.Should().Contain("imdbid=0076759");
        }

        [Test]
        public void should_not_use_aggregrated_id_search_if_no_ids_supported()
        {
            _capabilities.MovieSearchParams = new List<MovieSearchParam> { MovieSearchParam.Q };

            var results = Subject.GetSearchRequests(_movieSearchCriteria);
            results.Tiers.Should().Be(1);
            results.GetTier(0).Should().HaveCount(1);

            var page = results.GetTier(0).First().First();

            page.Url.Query.Should().Contain("q=");
        }

        [Test]
        public void should_not_use_aggregrated_id_search_if_no_ids_are_known()
        {
            _capabilities.MovieSearchParams = new List<MovieSearchParam> { MovieSearchParam.Q, MovieSearchParam.ImdbId };

            _movieSearchCriteria.ImdbId = null;

            var results = Subject.GetSearchRequests(_movieSearchCriteria);

            var page = results.GetTier(0).First().First();

            page.Url.Query.Should().Contain("q=");
        }

        [Test]
        public void should_fallback_to_q()
        {
            _capabilities.MovieSearchParams = new List<MovieSearchParam> { MovieSearchParam.Q, MovieSearchParam.ImdbId, MovieSearchParam.TmdbId };

            var results = Subject.GetSearchRequests(_movieSearchCriteria);
            results.Tiers.Should().Be(2);

            var pageTier2 = results.GetTier(1).First().First();

            pageTier2.Url.Query.Should().NotContain("tmdbid=11");
            pageTier2.Url.Query.Should().NotContain("imdbid=0076759");
            pageTier2.Url.Query.Should().Contain("q=");
        }
    }
}
