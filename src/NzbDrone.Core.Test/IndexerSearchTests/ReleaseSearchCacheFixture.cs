using System;
using System.Collections.Generic;
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
        public void should_not_cache_when_disabled()
        {
            _configService.SetupGet(c => c.SearchCacheEnabled).Returns(false);

            _subject.Set(_request, new List<int> { 1 }, false, new NewznabResults
            {
                Releases = new List<ReleaseInfo> { new() { Title = "A" } }
            });

            _subject.TryGet(_request, new List<int> { 1 }, false, out _).Should().BeFalse();
        }

        [Test]
        public void should_not_return_existing_results_after_disabled()
        {
            _subject.Set(_request, new List<int> { 1 }, false, new NewznabResults
            {
                Releases = new List<ReleaseInfo> { new() { Title = "A" } }
            });

            _configService.SetupGet(c => c.SearchCacheEnabled).Returns(false);

            _subject.TryGet(_request, new List<int> { 1 }, false, out _).Should().BeFalse();
        }

        [Test]
        public void should_store_results_with_configured_ttl()
        {
            _configService.SetupGet(c => c.SearchCacheTtl).Returns(15);

            var cache = CaptureCache();
            var subject = new ReleaseSearchCache(_configService.Object, cache.Manager);

            subject.Set(_request, new List<int> { 1 }, false, new NewznabResults
            {
                Releases = new List<ReleaseInfo> { new() { Title = "A" } }
            });

            cache.Cached.Verify(c => c.Set(It.IsAny<string>(), It.IsAny<NewznabResults>(), TimeSpan.FromMinutes(15)), Times.Once);
        }

        [Test]
        public void should_store_results_with_default_ttl_when_configured_ttl_is_invalid()
        {
            _configService.SetupGet(c => c.SearchCacheTtl).Returns(0);

            var cache = CaptureCache();
            var subject = new ReleaseSearchCache(_configService.Object, cache.Manager);

            subject.Set(_request, new List<int> { 1 }, false, new NewznabResults
            {
                Releases = new List<ReleaseInfo> { new() { Title = "A" } }
            });

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
        public void should_miss_when_query_differs()
        {
            _subject.Set(_request, new List<int> { 1 }, false, new NewznabResults { Releases = new List<ReleaseInfo>() });

            var other = new NewznabRequest { t = "tvsearch", q = "Other Show", tvdbid = 123, season = 1, ep = "2" };

            _subject.TryGet(other, new List<int> { 1 }, false, out _).Should().BeFalse();
        }

        [Test]
        public void should_miss_when_indexers_differ()
        {
            _subject.Set(_request, new List<int> { 1 }, false, new NewznabResults { Releases = new List<ReleaseInfo>() });

            _subject.TryGet(_request, new List<int> { 2 }, false, out _).Should().BeFalse();
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

        private static (ICacheManager Manager, Mock<ICached<NewznabResults>> Cached) CaptureCache()
        {
            var cached = new Mock<ICached<NewznabResults>>();
            var manager = new Mock<ICacheManager>();
            manager.Setup(c => c.GetCache<NewznabResults>(It.IsAny<Type>(), It.IsAny<string>())).Returns(cached.Object);

            return (manager.Object, cached);
        }
    }
}
