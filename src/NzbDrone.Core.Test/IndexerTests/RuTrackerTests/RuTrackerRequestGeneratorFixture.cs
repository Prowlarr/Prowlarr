using System;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Indexers.Definitions;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IndexerTests.RuTrackerTests
{
    [TestFixture]
    public class RuTrackerRequestGeneratorFixture : CoreTest
    {
        private RuTrackerRequestGenerator _generator;
        private IndexerCapabilities _capabilities;

        [SetUp]
        public void Setup()
        {
            _capabilities = Mocker.Resolve<RuTracker>().Capabilities;
            _generator = new RuTrackerRequestGenerator(new RuTrackerSettings { BaseUrl = "https://rutracker.invalid/" }, _capabilities);
        }

        [TestCase("Chainsmoker Cat", 1, "Chainsmoker%Cat")]
        [TestCase("Yani Neko", 1, "Yani%Neko")]
        [TestCase("Jujutsu Kaisen", 3, "Jujutsu%Kaisen")]
        [TestCase("Re:Zero", 4, "Re%Zero")]
        public void should_search_anime_season_by_title_without_guessing_tracker_season(string title, int season, string expectedTerm)
        {
            var criteria = new TvSearchCriteria
            {
                SearchTerm = title,
                Season = season,
                Categories = new[] { 5070 }
            };

            var requests = _generator.GetSearchRequests(criteria).GetAllTiers().SelectMany(page => page).ToArray();
            requests.Should().ContainSingle();
            var request = requests.Single();

            ParseUtil.GetArgumentFromQueryString(request.Url.FullUri, "nm").Should().Be(expectedTerm);
            ParseUtil.GetArgumentFromQueryString(request.Url.FullUri, "f").Should().Be(string.Join(",", _capabilities.Categories.MapTorznabCapsToTrackers(criteria.Categories)));
            request.HttpRequest.AllowAutoRedirect.Should().BeFalse();
            criteria.Season.Should().Be(season);
            criteria.SearchTerm.Should().Be(title);
            criteria.Categories.Should().Equal(5070);

            var basic = _generator.GetSearchRequests(new BasicSearchCriteria { SearchTerm = title, Categories = criteria.Categories });
            request.Url.FullUri.Should().Be(basic.GetAllTiers().Single().Single().Url.FullUri);
        }

        [TestCase(new[] { 5000 })]
        [TestCase(new[] { 5040 })]
        [TestCase(new[] { 2000 })]
        [TestCase(new[] { 5070, 5040 })]
        [TestCase(new[] { 5070, 5000 })]
        [TestCase(new[] { 5070, 2000 })]
        [TestCase(new[] { 101105 })]
        [TestCase(new[] { 5070, 101105 })]
        public void should_preserve_non_anime_mixed_and_custom_category_queries(int[] categories)
        {
            AssertSeasonSuffix(categories);
        }

        [Test]
        public void should_preserve_unrestricted_category_queries()
        {
            AssertSeasonSuffix(null);
            AssertSeasonSuffix(Array.Empty<int>());
        }

        [TestCase("12")]
        [TestCase("01/03")]
        public void should_preserve_episode_query_behavior(string episode)
        {
            var requests = _generator.GetSearchRequests(new TvSearchCriteria
            {
                SearchTerm = "Chainsmoker Cat",
                Season = 1,
                Episode = episode,
                Categories = new[] { 5070 }
            });

            ParseUtil.GetArgumentFromQueryString(requests.GetAllTiers().Single().Single().Url.FullUri, "nm")
                .Should().Be("Chainsmoker%Cat ТВ | Сезон: 1");
        }

        [TestCase(null)]
        [TestCase(0)]
        public void should_preserve_searches_without_a_positive_season(int? season)
        {
            var request = _generator.GetSearchRequests(new TvSearchCriteria
            {
                SearchTerm = "Chainsmoker Cat",
                Season = season,
                Categories = new[] { 5070 }
            }).GetAllTiers().Single().Single();

            ParseUtil.GetArgumentFromQueryString(request.Url.FullUri, "nm").Should().Be("Chainsmoker%Cat");
        }

        [TestCase(null)]
        [TestCase("")]
        public void should_not_add_a_title_filter_to_rss(string term)
        {
            var request = _generator.GetSearchRequests(new TvSearchCriteria
            {
                SearchTerm = term,
                Season = 1,
                Categories = new[] { 5070 }
            }).GetAllTiers().Single().Single();

            ParseUtil.GetArgumentFromQueryString(request.Url.FullUri, "nm").Should().BeNullOrEmpty();
        }

        [Test]
        public void should_preserve_movie_searches()
        {
            var criteria = new MovieSearchCriteria { SearchTerm = "The Matrix", Categories = new[] { 2040 } };
            var movie = _generator.GetSearchRequests(criteria).GetAllTiers().SelectMany(page => page).Select(request => request.Url.FullUri);
            var basic = _generator.GetSearchRequests(new BasicSearchCriteria { SearchTerm = criteria.SearchTerm, Categories = criteria.Categories })
                .GetAllTiers().SelectMany(page => page).Select(request => request.Url.FullUri);

            movie.Should().Equal(basic);
        }

        private void AssertSeasonSuffix(int[] categories)
        {
            var requests = _generator.GetSearchRequests(new TvSearchCriteria
            {
                SearchTerm = "Silo",
                Season = 3,
                Categories = categories
            }).GetAllTiers().SelectMany(page => page).ToArray();

            requests.Should().NotBeEmpty();
            foreach (var request in requests)
            {
                ParseUtil.GetArgumentFromQueryString(request.Url.FullUri, "nm").Should().Be("Silo ТВ | Сезон: 3");
            }
        }
    }
}
