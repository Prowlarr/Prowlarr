using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.IndexerSearch;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Test.IndexerSearchTests
{
    public class ReleaseSearchCacheFixture
    {
        private Mock<IConfigService> _configService;
        private ServiceProvider _provider;
        private ReleaseSearchCache _subject;
        private NewznabRequest _request;

        [SetUp]
        public void SetUp()
        {
            _configService = new Mock<IConfigService>();
            _configService.SetupGet(c => c.SearchCacheEnabled).Returns(true);
            _configService.SetupGet(c => c.SearchCacheTtl).Returns(5);

            var services = new ServiceCollection();
            services.AddHybridCache();
            _provider = services.BuildServiceProvider();

            _subject = new ReleaseSearchCache(_configService.Object, _provider.GetRequiredService<HybridCache>());
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

        [TearDown]
        public void TearDown()
        {
            _provider.Dispose();
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

            var cache = new RecordingHybridCache();
            var subject = new ReleaseSearchCache(_configService.Object, cache);

            await subject.GetOrSearch(_request, new List<int> { 1 }, false, () => SearchResults("A"));

            cache.LastOptions.Expiration.Should().Be(TimeSpan.FromMinutes(15));
            cache.LastOptions.Flags.Should().NotBeNull();
            cache.LastOptions.Flags.Value.Should().HaveFlag(HybridCacheEntryFlags.DisableDistributedCache);
        }

        [Test]
        public async Task should_store_results_with_default_ttl_when_configured_ttl_is_invalid()
        {
            _configService.SetupGet(c => c.SearchCacheTtl).Returns(0);

            var cache = new RecordingHybridCache();
            var subject = new ReleaseSearchCache(_configService.Object, cache);

            await subject.GetOrSearch(_request, new List<int> { 1 }, false, () => SearchResults("A"));

            cache.LastOptions.Expiration.Should().Be(TimeSpan.FromMinutes(5));
            cache.LastOptions.Flags.Should().NotBeNull();
            cache.LastOptions.Flags.Value.Should().HaveFlag(HybridCacheEntryFlags.DisableDistributedCache);
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
        public async Task should_preserve_torrent_info_across_cache_hits()
        {
            await _subject.GetOrSearch(_request, new List<int> { 1 }, false, () =>
            {
                return Task.FromResult(new NewznabResults
                {
                    Releases = new List<ReleaseInfo>
                    {
                        new TorrentInfo
                        {
                            Title = "A",
                            DownloadUrl = "http://indexer/dl",
                            InfoHash = "abc123",
                            Seeders = 7
                        }
                    }
                });
            });

            var cached = await _subject.GetOrSearch(_request, new List<int> { 1 }, false, ShouldNotSearch);
            var torrent = cached.Releases[0].Should().BeOfType<TorrentInfo>().Subject;

            torrent.InfoHash.Should().Be("abc123");
            torrent.Seeders.Should().Be(7);
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

        private sealed class RecordingHybridCache : HybridCache
        {
            public HybridCacheEntryOptions LastOptions { get; private set; }

            public override ValueTask<T> GetOrCreateAsync<TState, T>(string key, TState state, Func<TState, CancellationToken, ValueTask<T>> factory, HybridCacheEntryOptions options = null, IEnumerable<string> tags = null, CancellationToken cancellationToken = default)
            {
                LastOptions = options;
                return factory(state, cancellationToken);
            }

            public override ValueTask SetAsync<T>(string key, T value, HybridCacheEntryOptions options = null, IEnumerable<string> tags = null, CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public override ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default)
            {
                return ValueTask.CompletedTask;
            }

            public override ValueTask RemoveByTagAsync(string tag, CancellationToken cancellationToken = default)
            {
                return ValueTask.CompletedTask;
            }
        }
    }
}
