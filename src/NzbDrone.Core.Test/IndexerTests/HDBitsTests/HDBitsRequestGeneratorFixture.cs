using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using NzbDrone.Common.Http;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Indexers.Definitions.HDBits;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IndexerTests.HDBitsTests
{
    public class HDBitsRequestGeneratorFixture : CoreTest<HDBitsRequestGenerator>
    {
        private MovieSearchCriteria _movieSearchCriteria;
        private TvSearchCriteria _tvSearchSeasonEpisodeCriteria;
        private TvSearchCriteria _tvSearchDailyEpisodeCriteria;

        [SetUp]
        public void Setup()
        {
            Subject.Settings = new HDBitsSettings
            {
                ApiKey = "abcd",
                Username = "somename"
            };

            Subject.Capabilities = new IndexerCapabilities
            {
                TvSearchParams = new List<TvSearchParam>
                {
                    TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep, TvSearchParam.TvdbId
                },
                MovieSearchParams = new List<MovieSearchParam>
                {
                    MovieSearchParam.Q, MovieSearchParam.ImdbId
                },
            };

            Subject.Capabilities.Categories.AddCategoryMapping(6, NewznabStandardCategory.Audio, "Audio Track");
            Subject.Capabilities.Categories.AddCategoryMapping(3, NewznabStandardCategory.TVDocumentary, "Documentary");
            Subject.Capabilities.Categories.AddCategoryMapping(8, NewznabStandardCategory.Other, "Misc/Demo");
            Subject.Capabilities.Categories.AddCategoryMapping(1, NewznabStandardCategory.Movies, "Movie");
            Subject.Capabilities.Categories.AddCategoryMapping(4, NewznabStandardCategory.Audio, "Music");
            Subject.Capabilities.Categories.AddCategoryMapping(5, NewznabStandardCategory.TVSport, "Sport");
            Subject.Capabilities.Categories.AddCategoryMapping(2, NewznabStandardCategory.TV, "TV");
            Subject.Capabilities.Categories.AddCategoryMapping(7, NewznabStandardCategory.XXX, "XXX");

            _movieSearchCriteria = new MovieSearchCriteria
            {
                Categories = new[] { 2000, 2010 },
                ImdbId = "0076759"
            };

            _tvSearchSeasonEpisodeCriteria = new TvSearchCriteria
            {
                Categories = new[] { 5000, 5010 },
                TvdbId = 392256,
                Season = 1,
                Episode = "3"
            };

            _tvSearchDailyEpisodeCriteria = new TvSearchCriteria
            {
                Categories = new[] { 5000, 5010 },
                TvdbId = 289574,
                Season = 2023,
                Episode = "01/03"
            };
        }

        [Test]
        public void should_search_by_imdbid_if_supported()
        {
            var results = Subject.GetSearchRequests(_movieSearchCriteria);
            var imdbQuery = int.Parse(_movieSearchCriteria.ImdbId);

            results.GetAllTiers().Should().HaveCount(1);

            var page = results.GetAllTiers().First().First();

            var encoding = HttpHeader.GetEncodingFromContentType(page.HttpRequest.Headers.ContentType);

            var body = encoding.GetString(page.HttpRequest.ContentData);
            var query = JsonConvert.DeserializeObject<TorrentQuery>(body);

            query.Category.Should().HaveCount(1);
            query.ImdbInfo.Id.Should().Be(imdbQuery);
        }

        [Test]
        public void should_search_by_tvdbid_season_episode_if_supported()
        {
            var results = Subject.GetSearchRequests(_tvSearchSeasonEpisodeCriteria);
            var tvdbQuery = _tvSearchSeasonEpisodeCriteria.TvdbId;

            results.GetAllTiers().Should().HaveCount(1);

            var page = results.GetAllTiers().First().First();

            var encoding = HttpHeader.GetEncodingFromContentType(page.HttpRequest.Headers.ContentType);

            var body = encoding.GetString(page.HttpRequest.ContentData);
            var query = JsonConvert.DeserializeObject<TorrentQuery>(body);

            query.Category.Should().HaveCount(3);
            query.TvdbInfo.Id.Should().Be(tvdbQuery);
            query.Search.Should().BeNullOrWhiteSpace();
            query.TvdbInfo.Season.Should().Be(1);
            query.TvdbInfo.Episode.Should().Be("3");
        }

        [Test]
        public void should_search_by_tvdbid_daily_episode_if_supported()
        {
            var results = Subject.GetSearchRequests(_tvSearchDailyEpisodeCriteria);
            var tvdbQuery = _tvSearchDailyEpisodeCriteria.TvdbId;

            results.GetAllTiers().Should().HaveCount(1);

            var page = results.GetAllTiers().First().First();

            var encoding = HttpHeader.GetEncodingFromContentType(page.HttpRequest.Headers.ContentType);

            var body = encoding.GetString(page.HttpRequest.ContentData);
            var query = JsonConvert.DeserializeObject<TorrentQuery>(body);

            query.Category.Should().HaveCount(3);
            query.TvdbInfo.Id.Should().Be(tvdbQuery);
            query.Search.Should().Be("2023-01-03");
            query.TvdbInfo.Season.Should().BeNull();
            query.TvdbInfo.Episode.Should().BeNull();
        }
    }
}
