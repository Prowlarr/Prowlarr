using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Indexers.BroadcastheNet;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IndexerTests.BroadcastheNetTests
{
    public class BroadcastheNetRequestGeneratorFixture : CoreTest<BroadcastheNetRequestGenerator>
    {
        [SetUp]
        public void Setup()
        {
            Subject.Settings = new BroadcastheNetSettings
            {
                BaseUrl = "https://api.broadcasthe.net/",
                ApiKey = "abc"
            };

            Subject.Capabilities = new IndexerCapabilities
            {
                LimitsDefault = 100,
                LimitsMax = 1000,
                TvSearchParams = new List<TvSearchParam>
                {
                    TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep, TvSearchParam.TvdbId, TvSearchParam.RId
                }
            };
        }

        [Test]
        public void should_have_empty_parameters_if_rss_search()
        {
            var tvSearchCriteria = new TvSearchCriteria
            {
                Categories = new[] { NewznabStandardCategory.TV.Id, NewznabStandardCategory.TVHD.Id }
            };

            var results = Subject.GetSearchRequests(tvSearchCriteria);

            results.GetAllTiers().Should().HaveCount(1);

            var page = results.GetAllTiers().First().First();
            var query = ParseTorrentQueryFromRequest(page.HttpRequest);

            query.Tvdb.Should().BeNull();
            query.Tvrage.Should().BeNull();
            query.Search.Should().BeNullOrWhiteSpace();
            query.Category.Should().BeNullOrWhiteSpace();
            query.Name.Should().BeNullOrWhiteSpace();
        }

        [Test]
        public void should_search_by_tvdbid_season_if_supported()
        {
            var tvSearchCriteria = new TvSearchCriteria
            {
                Categories = new[] { NewznabStandardCategory.TV.Id, NewznabStandardCategory.TVHD.Id },
                TvdbId = 371980,
                Season = 1
            };

            var results = Subject.GetSearchRequests(tvSearchCriteria);

            results.Tiers.Should().Be(1);
            results.GetAllTiers().Should().HaveCount(2);

            var firstPage = results.GetAllTiers().First().First();
            var firstQuery = ParseTorrentQueryFromRequest(firstPage.HttpRequest);

            firstQuery.Tvdb.Should().Be("371980");
            firstQuery.Tvrage.Should().BeNull();
            firstQuery.Search.Should().BeNull();
            firstQuery.Category.Should().Be("Season");
            firstQuery.Name.Should().Be("Season 1%");

            var secondPage = results.GetAllTiers().Skip(1).First().First();
            var secondQuery = ParseTorrentQueryFromRequest(secondPage.HttpRequest);

            secondQuery.Tvdb.Should().Be("371980");
            secondQuery.Tvrage.Should().BeNull();
            secondQuery.Search.Should().BeNull();
            secondQuery.Category.Should().Be("Episode");
            secondQuery.Name.Should().Be("S01E%");
        }

        [Test]
        public void should_search_by_tvdbid_season_episode_if_supported()
        {
            var tvSearchCriteria = new TvSearchCriteria
            {
                Categories = new[] { NewznabStandardCategory.TV.Id, NewznabStandardCategory.TVHD.Id },
                TvdbId = 371980,
                Season = 1,
                Episode = "3"
            };

            var results = Subject.GetSearchRequests(tvSearchCriteria);

            results.Tiers.Should().Be(1);
            results.GetAllTiers().Should().HaveCount(1);

            var page = results.GetAllTiers().First().First();
            var query = ParseTorrentQueryFromRequest(page.HttpRequest);

            query.Tvdb.Should().Be("371980");
            query.Tvrage.Should().BeNull();
            query.Search.Should().BeNull();
            query.Category.Should().Be("Episode");
            query.Name.Should().Be("S01E03");
        }

        [Test]
        public void should_search_by_tvdbid_daily_episode_if_supported()
        {
            var tvSearchCriteria = new TvSearchCriteria
            {
                Categories = new[] { NewznabStandardCategory.TV.Id, NewznabStandardCategory.TVHD.Id },
                TvdbId = 289574,
                Season = 2023,
                Episode = "01/03"
            };

            var results = Subject.GetSearchRequests(tvSearchCriteria);

            results.Tiers.Should().Be(1);
            results.GetAllTiers().Should().HaveCount(1);

            var page = results.GetAllTiers().First().First();
            var query = ParseTorrentQueryFromRequest(page.HttpRequest);

            query.Tvdb.Should().Be("289574");
            query.Tvrage.Should().BeNull();
            query.Search.Should().BeNull();
            query.Category.Should().Be("Episode");
            query.Name.Should().Be("2023.01.03");
        }

        [Test]
        public void should_prefer_search_by_tvdbid_if_rid_supported()
        {
            var tvSearchCriteria = new TvSearchCriteria
            {
                Categories = new[] { NewznabStandardCategory.TV.Id, NewznabStandardCategory.TVHD.Id },
                TvdbId = 371980,
                RId = 12345
            };

            var results = Subject.GetSearchRequests(tvSearchCriteria);

            results.Tiers.Should().Be(1);
            results.GetAllTiers().Should().HaveCount(1);

            var page = results.GetAllTiers().First().First();
            var query = ParseTorrentQueryFromRequest(page.HttpRequest);

            query.Tvdb.Should().Be("371980");
            query.Tvrage.Should().BeNull();
            query.Search.Should().BeNull();
            query.Category.Should().BeNull();
            query.Name.Should().BeNull();
        }

        [Test]
        public void should_search_by_term_supported()
        {
            var tvSearchCriteria = new TvSearchCriteria
            {
                Categories = new[] { NewznabStandardCategory.TV.Id, NewznabStandardCategory.TVHD.Id },
                SearchTerm = "Malcolm in the Middle"
            };

            var results = Subject.GetSearchRequests(tvSearchCriteria);

            results.Tiers.Should().Be(1);
            results.GetAllTiers().Should().HaveCount(1);

            var page = results.GetAllTiers().First().First();
            var query = ParseTorrentQueryFromRequest(page.HttpRequest);

            query.Tvdb.Should().BeNull();
            query.Tvrage.Should().BeNull();
            query.Search.Should().Be("Malcolm%in%the%Middle");
            query.Category.Should().BeNull();
            query.Name.Should().BeNull();
        }

        [Test]
        public void should_search_by_term_season_if_supported()
        {
            var tvSearchCriteria = new TvSearchCriteria
            {
                Categories = new[] { NewznabStandardCategory.TV.Id, NewznabStandardCategory.TVHD.Id },
                SearchTerm = "Malcolm in the Middle",
                Season = 2
            };

            var results = Subject.GetSearchRequests(tvSearchCriteria);

            results.Tiers.Should().Be(1);
            results.GetAllTiers().Should().HaveCount(2);

            var firstPage = results.GetAllTiers().First().First();
            var firstQuery = ParseTorrentQueryFromRequest(firstPage.HttpRequest);

            firstQuery.Tvdb.Should().BeNull();
            firstQuery.Tvrage.Should().BeNull();
            firstQuery.Search.Should().Be("Malcolm%in%the%Middle");
            firstQuery.Category.Should().Be("Season");
            firstQuery.Name.Should().Be("Season 2%");

            var secondPage = results.GetAllTiers().Skip(1).First().First();
            var secondQuery = ParseTorrentQueryFromRequest(secondPage.HttpRequest);

            secondQuery.Tvdb.Should().BeNull();
            secondQuery.Tvrage.Should().BeNull();
            secondQuery.Search.Should().Be("Malcolm%in%the%Middle");
            secondQuery.Category.Should().Be("Episode");
            secondQuery.Name.Should().Be("S02E%");
        }

        [Test]
        public void should_search_by_term_season_episode_if_supported()
        {
            var tvSearchCriteria = new TvSearchCriteria
            {
                Categories = new[] { NewznabStandardCategory.TV.Id, NewznabStandardCategory.TVHD.Id },
                SearchTerm = "Malcolm in the Middle",
                Season = 2,
                Episode = "3"
            };

            var results = Subject.GetSearchRequests(tvSearchCriteria);

            results.Tiers.Should().Be(1);
            results.GetAllTiers().Should().HaveCount(1);

            var page = results.GetAllTiers().First().First();
            var query = ParseTorrentQueryFromRequest(page.HttpRequest);

            query.Tvdb.Should().BeNull();
            query.Tvrage.Should().BeNull();
            query.Search.Should().Be("Malcolm%in%the%Middle");
            query.Category.Should().Be("Episode");
            query.Name.Should().Be("S02E03");
        }

        [Test]
        public void should_search_by_term_daily_episode_if_supported()
        {
            var tvSearchCriteria = new TvSearchCriteria
            {
                Categories = new[] { NewznabStandardCategory.TV.Id, NewznabStandardCategory.TVHD.Id },
                SearchTerm = "The Late Show with Stephen Colbert",
                Season = 2023,
                Episode = "01/03"
            };

            var results = Subject.GetSearchRequests(tvSearchCriteria);

            results.Tiers.Should().Be(1);
            results.GetAllTiers().Should().HaveCount(1);

            var page = results.GetAllTiers().First().First();
            var query = ParseTorrentQueryFromRequest(page.HttpRequest);

            query.Tvdb.Should().BeNull();
            query.Tvrage.Should().BeNull();
            query.Search.Should().Be("The%Late%Show%with%Stephen%Colbert");
            query.Category.Should().Be("Episode");
            query.Name.Should().Be("2023.01.03");
        }

        private static BroadcastheNetTorrentQuery ParseTorrentQueryFromRequest(HttpRequest httpRequest)
        {
            var encoding = HttpHeader.GetEncodingFromContentType(httpRequest.Headers.ContentType);
            var body = encoding.GetString(httpRequest.ContentData);

            var rpcBody = JsonConvert.DeserializeObject<Dictionary<string, object>>(body);

            return JsonConvert.DeserializeObject<BroadcastheNetTorrentQuery>(((JArray)rpcBody["params"])[1].ToJson());
        }
    }
}
