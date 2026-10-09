using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Cache;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.IndexerSearch;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Test.IndexerSearchTests
{
    public class ReleaseSearchCacheFixture
    {
        private Mock<IConfigService> _configService;
        private ReleaseSearchCache _subject;
        private NewznabRequest _request;

        [SetUp]
        public void SetUp()
        {
            _configService = new Mock<IConfigService>();
            _configService.SetupGet(c => c.SearchCacheEnabled).Returns(true);
            _configService.SetupGet(c => c.SearchCacheTtl).Returns(5);

            _subject = new ReleaseSearchCache(_configService.Object, new CacheManager());
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
        public async Task should_return_cached_results_for_same_search()
        {
            await _subject.GetOrSearch(_request, new List<int> { 1 }, false, () => SearchResults("Some Show S01E02"));

            var cached = await _subject.GetOrSearch(_request, new List<int> { 1 }, false, ShouldNotSearch);

            cached.Releases.Should().HaveCount(1);
            cached.Releases[0].Title.Should().Be("Some Show S01E02");
        }

        [Test]
        public async Task should_not_cache_when_disabled()
        {
            _configService.SetupGet(c => c.SearchCacheEnabled).Returns(false);

            var searches = 0;

            await _subject.GetOrSearch(_request, new List<int> { 1 }, false, () =>
            {
                searches++;
                return SearchResults("A");
            });

            await _subject.GetOrSearch(_request, new List<int> { 1 }, false, () =>
            {
                searches++;
                return SearchResults("A");
            });

            searches.Should().Be(2);
        }

        [Test]
        public async Task should_not_return_existing_results_after_disabled()
        {
            await _subject.GetOrSearch(_request, new List<int> { 1 }, false, () => SearchResults("A"));

            _configService.SetupGet(c => c.SearchCacheEnabled).Returns(false);

            var searched = false;
            await _subject.GetOrSearch(_request, new List<int> { 1 }, false, () =>
            {
                searched = true;
                return SearchResults("A");
            });

            searched.Should().BeTrue();
        }

        [Test]
        public async Task should_store_results_with_configured_ttl()
        {
            _configService.SetupGet(c => c.SearchCacheTtl).Returns(15);

            var cache = CaptureCache();
            var subject = new ReleaseSearchCache(_configService.Object, cache.Manager);

            await subject.GetOrSearch(_request, new List<int> { 1 }, false, () => SearchResults("A"));

            cache.Cached.Verify(c => c.Set(It.IsAny<string>(), It.IsAny<NewznabResults>(), TimeSpan.FromMinutes(15)), Times.Once);
        }

        [Test]
        public async Task should_not_extend_ttl_on_cache_hit()
        {
            var cache = CaptureCache();
            cache.Cached.Setup(c => c.Find(It.IsAny<string>())).Returns(new NewznabResults
            {
                Releases = new List<ReleaseInfo> { new() { Title = "A" } }
            });

            var subject = new ReleaseSearchCache(_configService.Object, cache.Manager);

            await subject.GetOrSearch(_request, new List<int> { 1 }, false, ShouldNotSearch);

            cache.Cached.Verify(c => c.Set(It.IsAny<string>(), It.IsAny<NewznabResults>(), It.IsAny<TimeSpan>()), Times.Never);
        }

        [Test]
        public async Task should_store_results_with_default_ttl_when_configured_ttl_is_invalid()
        {
            _configService.SetupGet(c => c.SearchCacheTtl).Returns(0);

            var cache = CaptureCache();
            var subject = new ReleaseSearchCache(_configService.Object, cache.Manager);

            await subject.GetOrSearch(_request, new List<int> { 1 }, false, () => SearchResults("A"));

            cache.Cached.Verify(c => c.Set(It.IsAny<string>(), It.IsAny<NewznabResults>(), TimeSpan.FromMinutes(5)), Times.Once);
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
        public async Task should_miss_when_query_differs()
        {
            await _subject.GetOrSearch(_request, new List<int> { 1 }, false, () => SearchResults("A"));

            var other = new NewznabRequest { t = "tvsearch", q = "Other Show", tvdbid = 123, season = 1, ep = "2" };

            var searched = false;
            await _subject.GetOrSearch(other, new List<int> { 1 }, false, () =>
            {
                searched = true;
                return SearchResults("B");
            });

            searched.Should().BeTrue();
        }

        [Test]
        public async Task should_miss_when_indexers_differ()
        {
            await _subject.GetOrSearch(_request, new List<int> { 1 }, false, () => SearchResults("A"));

            var searched = false;
            await _subject.GetOrSearch(_request, new List<int> { 2 }, false, () =>
            {
                searched = true;
                return SearchResults("A");
            });

            searched.Should().BeTrue();
        }

        [Test]
        public async Task should_clone_releases_so_callers_cannot_mutate_the_cache()
        {
            await _subject.GetOrSearch(_request, new List<int> { 1 }, false, () => SearchResults("A"));

            var cached = await _subject.GetOrSearch(_request, new List<int> { 1 }, false, ShouldNotSearch);
            cached.Releases[0].DownloadUrl = "http://mutated";

            var again = await _subject.GetOrSearch(_request, new List<int> { 1 }, false, ShouldNotSearch);
            again.Releases[0].DownloadUrl.Should().Be("http://indexer/dl");
        }

        [Test]
        public async Task should_run_one_search_for_concurrent_identical_requests()
        {
            var searchStarted = new TaskCompletionSource<bool>();
            var finishSearch = new TaskCompletionSource<bool>();
            var searches = 0;

            async Task<NewznabResults> Search()
            {
                Interlocked.Increment(ref searches);
                searchStarted.TrySetResult(true);
                await finishSearch.Task;

                return new NewznabResults { Releases = new List<ReleaseInfo> { new() { Title = "A" } } };
            }

            var first = _subject.GetOrSearch(_request, new List<int> { 1 }, false, Search);
            await searchStarted.Task;

            var second = _subject.GetOrSearch(_request, new List<int> { 1 }, false, Search);
            finishSearch.SetResult(true);

            var results = await Task.WhenAll(first, second);

            searches.Should().Be(1);
            results[0].Releases[0].Title.Should().Be("A");
            results[0].Releases[0].Should().NotBeSameAs(results[1].Releases[0]);
        }

        private static Task<NewznabResults> SearchResults(string title)
        {
            return Task.FromResult(new NewznabResults
            {
                Releases = new List<ReleaseInfo> { new() { Title = title, DownloadUrl = "http://indexer/dl" } }
            });
        }

        private static Task<NewznabResults> ShouldNotSearch()
        {
            throw new InvalidOperationException("indexer search should not run");
        }

        private static (ICacheManager Manager, Mock<ICached<NewznabResults>> Cached) CaptureCache()
        {
            var cached = new Mock<ICached<NewznabResults>>();
            var manager = new Mock<ICacheManager>();
            manager.Setup(c => c.GetCache<NewznabResults>(It.IsAny<Type>(), It.IsAny<string>())).Returns(cached.Object);

            return (manager.Object, cached);
        }
    }
}
