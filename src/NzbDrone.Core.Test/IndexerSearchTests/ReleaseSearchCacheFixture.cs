using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.IndexerSearch;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Test.IndexerSearchTests
{
    public class ReleaseSearchCacheFixture
    {
        private ReleaseSearchCache _subject;
        private NewznabRequest _request;

        [SetUp]
        public void SetUp()
        {
            _subject = new ReleaseSearchCache();
            _request = new NewznabRequest
            {
                t = "tvsearch",
                q = "Some Show",
                tvdbid = 123,
                season = 1,
                ep = "2",
                source = "Sonarr"
            };
        }

        [Test]
        public void should_return_cached_results_for_same_search()
        {
            var results = new NewznabResults
            {
                Releases = new List<ReleaseInfo> { new() { Title = "Some Show S01E02", DownloadUrl = "http://indexer/dl" } }
            };

            _subject.Set(_request, new List<int> { 1 }, false, results);

            _subject.TryGet(_request, new List<int> { 1 }, false, out var cached).Should().BeTrue();
            cached.Releases.Should().HaveCount(1);
            cached.Releases[0].Title.Should().Be("Some Show S01E02");
        }

        [Test]
        public void should_not_treat_source_host_or_server_as_part_of_the_key()
        {
            var first = new NewznabRequest { t = "movie", q = "Film", imdbid = "123", source = "Sonarr", host = "a", server = "http://a" };
            var second = new NewznabRequest { t = "movie", q = "Film", imdbid = "123", source = "Radarr", host = "b", server = "http://b" };

            ReleaseSearchCache.BuildKey(first, new List<int> { 1 }, false)
                .Should()
                .Be(ReleaseSearchCache.BuildKey(second, new List<int> { 1 }, false));
        }

        [Test]
        public void should_miss_when_query_differs()
        {
            _subject.Set(_request, new List<int> { 1 }, false, new NewznabResults { Releases = new List<ReleaseInfo>() });

            var other = new NewznabRequest { t = "tvsearch", q = "Other Show", tvdbid = 123, season = 1, ep = "2" };

            _subject.TryGet(other, new List<int> { 1 }, false, out _).Should().BeFalse();
        }

        [Test]
        public void should_clone_releases_so_callers_cannot_mutate_the_cache()
        {
            var results = new NewznabResults
            {
                Releases = new List<ReleaseInfo> { new() { Title = "A", DownloadUrl = "http://indexer/dl" } }
            };

            _subject.Set(_request, new List<int> { 1 }, false, results);

            _subject.TryGet(_request, new List<int> { 1 }, false, out var cached);
            cached.Releases[0].DownloadUrl = "http://mutated";

            _subject.TryGet(_request, new List<int> { 1 }, false, out var again);
            again.Releases[0].DownloadUrl.Should().Be("http://indexer/dl");
        }
    }
}
