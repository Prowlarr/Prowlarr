using System.Collections.Generic;
using System.Linq;
using System.Web;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Indexers.Definitions;
using NzbDrone.Core.Indexers.Definitions.Avistaz;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IndexerTests.AvistazTests
{
    [TestFixture(typeof(AvistaZ))]
    [TestFixture(typeof(CinemaZ))]
    [TestFixture(typeof(PrivateHD))]
    [TestFixture(typeof(ExoticaZ))]
    [TestFixture(typeof(AnimeZ))]
    public class AvistazLanguageFilterFixture<TIndexer> : CoreTest<TIndexer>
        where TIndexer : AvistazBase
    {
        private AvistazSettings _settings;

        [SetUp]
        public void Setup()
        {
            _settings = new AvistazSettings { Username = "someuser", Password = "somepass", Pid = "somepid" };

            Subject.Definition = new IndexerDefinition
            {
                Name = typeof(TIndexer).Name,
                Settings = _settings
            };
        }

        private IEnumerable<IndexerRequest> GetRequests(SearchCriteriaBase searchCriteria)
        {
            var generator = Subject.GetRequestGenerator();

            var chain = searchCriteria switch
            {
                MovieSearchCriteria movie => generator.GetSearchRequests(movie),
                TvSearchCriteria tv => generator.GetSearchRequests(tv),
                _ => generator.GetSearchRequests((BasicSearchCriteria)searchCriteria)
            };

            return chain.GetAllTiers().SelectMany(tier => tier).ToList();
        }

        private static IEnumerable<SearchCriteriaBase> SearchCriteria()
        {
            if (typeof(TIndexer) != typeof(ExoticaZ))
            {
                yield return new MovieSearchCriteria { SearchTerm = "test", Categories = new[] { 2000 } };
                yield return new TvSearchCriteria { SearchTerm = "test", Categories = new[] { 5000 } };
            }

            yield return new BasicSearchCriteria { SearchTerm = "test", Categories = new[] { 2000 } };
        }

        private static string[] GetQueryValues(IndexerRequest request, string key)
        {
            var query = HttpUtility.ParseQueryString(request.HttpRequest.Url.Query);

            return query.GetValues(key) ?? [];
        }

        [TestCaseSource(nameof(SearchCriteria))]
        public void should_not_add_language_parameters_by_default(SearchCriteriaBase searchCriteria)
        {
            var requests = GetRequests(searchCriteria);

            requests.Should().ContainSingle();
            GetQueryValues(requests.First(), "language[]").Should().BeEmpty();
            GetQueryValues(requests.First(), "subtitle[]").Should().BeEmpty();
        }

        [TestCaseSource(nameof(SearchCriteria))]
        public void should_add_selected_audio_and_subtitle_languages(SearchCriteriaBase searchCriteria)
        {
            _settings.SearchAudioLanguages = new[] { (int)AvistaZAudioLanguage.English, (int)AvistaZAudioLanguage.Japanese };
            _settings.SearchSubtitleLanguages = new[] { (int)AvistaZSubtitleLanguage.Basque, (int)AvistaZSubtitleLanguage.Catalan };

            var requests = GetRequests(searchCriteria);

            requests.Should().ContainSingle();
            GetQueryValues(requests.First(), "language[]").Should().BeEquivalentTo(new[] { "45", "79" });
            GetQueryValues(requests.First(), "subtitle[]").Should().BeEquivalentTo(new[] { "17", "28" });
        }

        [Test]
        public void should_add_only_the_selected_kind_of_language()
        {
            _settings.SearchSubtitleLanguages = new[] { (int)AvistaZSubtitleLanguage.English };

            var requests = GetRequests(new BasicSearchCriteria { SearchTerm = "test", Categories = new[] { 2000 } });

            GetQueryValues(requests.Single(), "language[]").Should().BeEmpty();
            GetQueryValues(requests.Single(), "subtitle[]").Should().BeEquivalentTo(new[] { "45" });
        }

        [Test]
        public void should_not_repeat_duplicated_languages()
        {
            _settings.SearchAudioLanguages = new[] { 45, 45, 79 };

            var requests = GetRequests(new BasicSearchCriteria { SearchTerm = "test", Categories = new[] { 2000 } });

            GetQueryValues(requests.Single(), "language[]").Should().BeEquivalentTo(new[] { "45", "79" });
        }
    }
}
