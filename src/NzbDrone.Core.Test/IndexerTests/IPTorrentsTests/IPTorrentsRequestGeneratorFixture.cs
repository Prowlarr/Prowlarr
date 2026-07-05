using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Indexers.Definitions;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IndexerTests.IPTorrentsTests
{
    public class IPTorrentsRequestGeneratorFixture : CoreTest<IPTorrentsRequestGenerator>
    {
        private MovieSearchCriteria _movieSearchCriteria;

        [SetUp]
        public void Setup()
        {
            Subject.Settings = new IPTorrentsSettings
            {
                BaseUrl = "https://iptorrents.com/",
                Cookie = "uid=123; pass=abc",
                UserAgent = "Mozilla/5.0"
            };

            Subject.Capabilities = new IndexerCapabilities
            {
                MovieSearchParams = new List<MovieSearchParam>
                {
                    MovieSearchParam.Q, MovieSearchParam.ImdbId
                }
            };

            Subject.Capabilities.Categories.AddCategoryMapping(72, NewznabStandardCategory.Movies, "Movies");

            _movieSearchCriteria = new MovieSearchCriteria
            {
                Categories = new[] { 2000 },
                ImdbId = "0076759"
            };
        }

        [Test]
        public void should_not_sort_by_seeders_by_default()
        {
            var results = Subject.GetSearchRequests(_movieSearchCriteria);

            var page = results.GetAllTiers().First().First();

            page.Url.FullUri.Should().NotContain("o=seeders");
        }

        [Test]
        public void should_sort_by_seeders_when_enabled()
        {
            Subject.Settings.SearchSortBy = (int)IPTorrentsSort.Seeders;

            var results = Subject.GetSearchRequests(_movieSearchCriteria);

            var page = results.GetAllTiers().First().First();

            page.Url.FullUri.Should().Contain("o=seeders");
        }

        [Test]
        public void should_not_sort_rss_feed_even_when_enabled()
        {
            Subject.Settings.SearchSortBy = (int)IPTorrentsSort.Seeders;

            var results = Subject.GetSearchRequests(new BasicSearchCriteria { Categories = new[] { 2000 } });

            var page = results.GetAllTiers().First().First();

            page.Url.FullUri.Should().NotContain("o=seeders");
        }
    }
}
