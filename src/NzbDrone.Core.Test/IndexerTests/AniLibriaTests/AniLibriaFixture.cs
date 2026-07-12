using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Http;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Indexers.Definitions;
using NzbDrone.Core.Indexers.Exceptions;
using NzbDrone.Core.Indexers.Settings;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IndexerTests.AniLibriaTests
{
    [TestFixture]
    public class AniLibriaFixture : CoreTest<AniLibria>
    {
        private readonly List<string> _requests = new();

        [SetUp]
        public void Setup()
        {
            Subject.Definition = new IndexerDefinition
            {
                Name = "AniLibria",
                Settings = new NoAuthTorrentBaseSettings
                {
                    BaseUrl = "https://aniliberty.top/"
                }
            };
        }

        [Test]
        public async Task should_make_a_search_request_then_one_batch_release_request_and_map_all_torrents()
        {
            Subject.IndexerUrls.Should().Equal("https://aniliberty.top/");

            SetupResponses(
                ReadAllText(@"Files/Indexers/AniLibria/search-releases.json"),
                ReadAllText(@"Files/Indexers/AniLibria/releases-list.json"),
                ReadAllText(@"Files/Indexers/AniLibria/recent-torrents.json"));

            var releases = (await Subject.Fetch(new BasicSearchCriteria { SearchTerm = "Test Anime!" })).Releases.Cast<TorrentInfo>().ToList();

            _requests.Should().Equal(
                "https://aniliberty.top/api/v1/app/search/releases?query=Test%20Anime",
                "https://aniliberty.top/api/v1/anime/releases/list?ids=100,200,300,400,500,600,700,800,900,1000");
            releases.Should().HaveCount(11);

            var avc = releases.Single(r => r.Title.Contains("Test TV", StringComparison.Ordinal) && r.Title.Contains("[AVC]", StringComparison.Ordinal));
            avc.Title.Should().Be("[AniLibria] Test TV S01E01-03 [WEB-DL][1080p][AVC]");
            avc.Guid.Should().Be("https://aniliberty.top/api/v1/anime/torrents/1001");
            avc.InfoUrl.Should().Be("https://aniliberty.top/api/v1/anime/releases/release/test-tv-pack");
            avc.DownloadUrl.Should().Be("https://aniliberty.top/api/v1/anime/torrents/1001/file");
            avc.MagnetUrl.Should().Be("magnet:?xt=urn:btih:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
            avc.InfoHash.Should().Be("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
            avc.Size.Should().Be(1234567890);
            avc.Seeders.Should().Be(12);
            avc.Peers.Should().Be(15);
            avc.Grabs.Should().Be(45);
            avc.PublishDate.Should().Be(DateTime.Parse("2026-01-02T03:04:05+00:00").ToUniversalTime());
            avc.Categories.Select(c => c.Id).Should().Contain(NewznabStandardCategory.TVAnime.Id);
            avc.Resolution.Should().Be("1080p");
            avc.Source.Should().Be("WEB-DL");
            avc.Codec.Should().Be("AVC");
            avc.DownloadVolumeFactor.Should().Be(0);
            avc.UploadVolumeFactor.Should().Be(1);

            releases.Single(r => r.Title.Contains("Test TV", StringComparison.Ordinal) && r.Title.Contains("[HEVC]", StringComparison.Ordinal)).Codec.Should().Be("HEVC");
            releases.Count(r => r.Title.Contains("S01E01-03", StringComparison.Ordinal)).Should().Be(2, "codec variants must be separate torrent results");
            releases.Single(r => r.Title.Contains("Test Movie", StringComparison.Ordinal)).Categories.Select(c => c.Id).Should().Contain(NewznabStandardCategory.MoviesOther.Id);
            releases.Single(r => r.Title.Contains("Test Movie", StringComparison.Ordinal)).Resolution.Should().Be("2160p");
            releases.Single(r => r.Title.Contains("Test OVA", StringComparison.Ordinal)).Categories.Select(c => c.Id).Should().Contain(NewznabStandardCategory.TVAnime.Id);
            releases.Single(r => r.Title.Contains("Test OVA", StringComparison.Ordinal)).Resolution.Should().Be("1440p");
            releases.Single(r => r.Title.Contains("Test Dorama", StringComparison.Ordinal)).Categories.Select(c => c.Id).Should().Contain(NewznabStandardCategory.TV.Id);
            releases.Single(r => r.Title.Contains("Test ONA", StringComparison.Ordinal)).Categories.Select(c => c.Id).Should().Contain(NewznabStandardCategory.TVAnime.Id);
            releases.Single(r => r.Title.Contains("Test OAD", StringComparison.Ordinal)).Categories.Select(c => c.Id).Should().Contain(NewznabStandardCategory.TVAnime.Id);
            releases.Single(r => r.Title.Contains("Test Special", StringComparison.Ordinal)).Categories.Select(c => c.Id).Should().Contain(NewznabStandardCategory.TVAnime.Id);
            releases.Single(r => r.Title.Contains("Test Web", StringComparison.Ordinal)).Categories.Select(c => c.Id).Should().Contain(NewznabStandardCategory.TVAnime.Id);

            var noType = releases.Single(r => r.Title.Contains("Test No Type", StringComparison.Ordinal));
            noType.Categories.Select(c => c.Id).Should().Contain(NewznabStandardCategory.OtherMisc.Id);
            noType.InfoUrl.Should().Be("https://aniliberty.top/api/v1/anime/releases/400");
            noType.Resolution.Should().Be("4320p");
            releases.Single(r => r.Title.Contains("Test Unknown", StringComparison.Ordinal)).Categories.Select(c => c.Id).Should().Contain(NewznabStandardCategory.OtherMisc.Id);
        }

        [Test]
        public async Task should_not_request_batch_details_for_an_empty_search_result()
        {
            SetupResponses(
                ReadAllText(@"Files/Indexers/AniLibria/search-empty.json"),
                ReadAllText(@"Files/Indexers/AniLibria/releases-list.json"),
                ReadAllText(@"Files/Indexers/AniLibria/recent-torrents.json"));

            var releases = (await Subject.Fetch(new MovieSearchCriteria { SearchTerm = "Not Found" })).Releases;

            releases.Should().BeEmpty();
            _requests.Should().ContainSingle().Which.Should().Be("https://aniliberty.top/api/v1/app/search/releases?query=Not%20Found");
        }

        [Test]
        public async Task should_not_issue_an_empty_query_search_and_use_the_recent_torrent_endpoint_for_rss()
        {
            SetupResponses(
                ReadAllText(@"Files/Indexers/AniLibria/search-releases.json"),
                ReadAllText(@"Files/Indexers/AniLibria/releases-list.json"),
                ReadAllText(@"Files/Indexers/AniLibria/recent-torrents.json"));

            var releases = (await Subject.Fetch(new BasicSearchCriteria())).Releases.Cast<TorrentInfo>().ToList();

            releases.Should().HaveCount(3);
            _requests.Should().ContainSingle().Which.Should().Be("https://aniliberty.top/api/v1/anime/torrents?limit=50");

            var recent = releases.Single(r => r.Guid.EndsWith("/9001", StringComparison.Ordinal));
            recent.Title.Should().Be("[AniLibria] Recent Test TV S01E01 [WEB-DL][1080p][AVC]");
            recent.Guid.Should().Be("https://aniliberty.top/api/v1/anime/torrents/9001");
            recent.DownloadUrl.Should().Be("https://aniliberty.top/api/v1/anime/torrents/9001/file");
            recent.InfoHash.Should().Be("9999999999999999999999999999999999999999");
            recent.Categories.Select(c => c.Id).Should().Contain(NewznabStandardCategory.TVAnime.Id);
            recent.PublishDate.Should().Be(new DateTime(2026, 7, 1, 9, 34, 56, DateTimeKind.Utc));
            releases.Select(r => r.Guid).Should().BeEquivalentTo(
                "https://aniliberty.top/api/v1/anime/torrents/9001",
                "https://aniliberty.top/api/v1/anime/torrents/9002",
                "https://aniliberty.top/api/v1/anime/torrents/9003");
        }

        [Test]
        public async Task should_use_the_configured_base_url_for_requests_and_torrent_urls()
        {
            Subject.Definition.Settings = new NoAuthTorrentBaseSettings
            {
                BaseUrl = "https://custom.anilibria.example/"
            };
            SetupResponses(
                ReadAllText(@"Files/Indexers/AniLibria/search-releases.json"),
                ReadAllText(@"Files/Indexers/AniLibria/releases-list.json"),
                ReadAllText(@"Files/Indexers/AniLibria/recent-torrents.json"));

            var releases = (await Subject.Fetch(new BasicSearchCriteria { SearchTerm = "Test Anime!" })).Releases.Cast<TorrentInfo>().ToList();

            _requests.Should().Equal(
                "https://custom.anilibria.example/api/v1/app/search/releases?query=Test%20Anime",
                "https://custom.anilibria.example/api/v1/anime/releases/list?ids=100,200,300,400,500,600,700,800,900,1000");
            var torrent = releases.Single(r => r.Title.Contains("Test TV", StringComparison.Ordinal) && r.Title.Contains("[AVC]", StringComparison.Ordinal));
            torrent.Guid.Should().Be("https://custom.anilibria.example/api/v1/anime/torrents/1001");
            torrent.DownloadUrl.Should().Be("https://custom.anilibria.example/api/v1/anime/torrents/1001/file");
            torrent.InfoUrl.Should().Be("https://custom.anilibria.example/api/v1/anime/releases/release/test-tv-pack");
        }

        [Test]
        [TestCase(null)]
        [TestCase("not-a-date")]
        public void should_keep_torrents_with_missing_or_invalid_created_at(string createdAt)
        {
            var createdAtJson = createdAt == null ? "null" : $"\"{createdAt}\"";
            var response = CreateIndexerResponse("{\"data\":[{\"id\":100,\"alias\":\"test\",\"type\":{\"value\":\"TV\"},\"torrents\":[{\"id\":2,\"hash\":\"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa\",\"label\":\"valid\",\"size\":1,\"seeders\":0,\"leechers\":0,\"completed_times\":0,\"created_at\":" + createdAtJson + "}]}]}");
            var before = DateTime.UtcNow;

            var release = Subject.GetParser().ParseResponse(response).Single();

            var after = DateTime.UtcNow;
            release.PublishDate.Kind.Should().Be(DateTimeKind.Utc);
            release.PublishDate.Should().BeOnOrAfter(before);
            release.PublishDate.Should().BeOnOrBefore(after);
        }

        [Test]
        [TestCase(0)]
        [TestCase(-1)]
        public void should_keep_torrents_without_an_alias_or_positive_release_id(long releaseId)
        {
            var response = CreateIndexerResponse($"{{\"data\":[{{\"id\":{releaseId},\"type\":{{\"value\":\"TV\"}},\"torrents\":[{{\"id\":2,\"hash\":\"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa\",\"label\":\"valid\",\"size\":1,\"seeders\":0,\"leechers\":0,\"completed_times\":0,\"created_at\":\"2026-01-02T03:04:05+00:00\"}}]}}]}}");

            var release = Subject.GetParser().ParseResponse(response).Single();

            release.InfoUrl.Should().BeNull();
        }

        [Test]
        public void should_use_the_release_title_when_a_torrent_label_is_null()
        {
            var response = CreateIndexerResponse("{\"data\":[{\"id\":100,\"alias\":\"test\",\"name\":{\"main\":\"Release title\"},\"type\":{\"value\":\"TV\"},\"torrents\":[{\"id\":2,\"hash\":\"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa\",\"label\":null,\"size\":1,\"seeders\":0,\"leechers\":0,\"completed_times\":0,\"created_at\":\"2026-01-02T03:04:05+00:00\"}]}]}");

            var release = Subject.GetParser().ParseResponse(response).Single();

            release.Title.Should().Be("Release title");
        }

        [Test]
        public void should_throw_indexer_exception_for_a_non_object_torrent()
        {
            var response = CreateIndexerResponse("{\"data\":[{\"id\":100,\"alias\":\"test\",\"type\":{\"value\":\"TV\"},\"torrents\":[123,{\"id\":2,\"hash\":\"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa\",\"label\":\"valid\",\"size\":1,\"seeders\":0,\"leechers\":0,\"completed_times\":0,\"created_at\":\"2026-01-02T03:04:05+00:00\"}]}]}");

            Action act = () => Subject.GetParser().ParseResponse(response);

            act.Should().Throw<IndexerException>().WithMessage("*Unable to parse AniLibria API response*");
        }

        [Test]
        public void should_throw_indexer_exception_for_malformed_search_json()
        {
            var response = CreateSearchIndexerResponse("{not-json");

            Action act = () => AniLibriaParser.ParseSearchReleaseIds(response);

            act.Should().Throw<IndexerException>().WithMessage("*Unable to parse AniLibria search response*");
        }

        [Test]
        public void should_throw_indexer_exception_for_a_non_array_search_response()
        {
            var response = CreateSearchIndexerResponse("{\"data\":[]}");

            Action act = () => AniLibriaParser.ParseSearchReleaseIds(response);

            act.Should().Throw<IndexerException>().WithMessage("*Unable to parse AniLibria search response*");
        }

        [Test]
        public void should_parse_a_data_enveloped_batch_response()
        {
            var response = CreateIndexerResponse("{\"data\":[{\"id\":100,\"alias\":\"test\",\"type\":{\"value\":\"TV\"},\"torrents\":[{\"id\":2,\"hash\":\"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa\",\"label\":\"valid\",\"size\":1,\"seeders\":0,\"leechers\":0,\"completed_times\":0,\"created_at\":\"2026-01-02T03:04:05+00:00\"}]}],\"meta\":{\"pagination\":{\"per_page\":50}}}");

            var releases = Subject.GetParser().ParseResponse(response);

            releases.Should().ContainSingle().Which.Title.Should().Be("valid");
        }

        [Test]
        [TestCase("{}")]
        public void should_throw_indexer_exception_for_a_malformed_batch_response_envelope(string content)
        {
            var response = CreateIndexerResponse(content);

            Action act = () => Subject.GetParser().ParseResponse(response);

            act.Should().Throw<IndexerException>().WithMessage("*expected an object with a data JSON array*");
        }

        [Test]
        [TestCase("[]")]
        [TestCase("{\"data\":{}}")]
        public void should_throw_indexer_exception_for_an_invalid_batch_response_shape(string content)
        {
            var response = CreateIndexerResponse(content);

            Action act = () => Subject.GetParser().ParseResponse(response);

            act.Should().Throw<IndexerException>().WithMessage("*Unable to parse AniLibria API response*");
        }

        [Test]
        [TestCase("{}")]
        [TestCase("{\"data\":null}")]
        public void should_throw_indexer_exception_for_a_malformed_recent_response_envelope(string content)
        {
            var response = CreateRecentIndexerResponse(content);

            Action act = () => Subject.GetParser().ParseResponse(response);

            act.Should().Throw<IndexerException>().WithMessage("*expected an object with a data JSON array*");
        }

        [Test]
        [TestCase("[]")]
        [TestCase("{\"data\":{}}")]
        public void should_throw_indexer_exception_for_an_invalid_recent_response_shape(string content)
        {
            var response = CreateRecentIndexerResponse(content);

            Action act = () => Subject.GetParser().ParseResponse(response);

            act.Should().Throw<IndexerException>().WithMessage("*Unable to parse AniLibria API response*");
        }

        [Test]
        public void should_throw_indexer_exception_for_a_non_object_search_entry()
        {
            var response = CreateSearchIndexerResponse("[123]");

            Action act = () => AniLibriaParser.ParseSearchReleaseIds(response);

            act.Should().Throw<IndexerException>().WithMessage("*Unable to parse AniLibria search response*");
        }

        [Test]
        public void should_throw_indexer_exception_for_a_non_object_release_entry()
        {
            var response = CreateIndexerResponse("{\"data\":[123]}");

            Action act = () => Subject.GetParser().ParseResponse(response);

            act.Should().Throw<IndexerException>().WithMessage("*Unable to parse AniLibria API response*");
        }

        [Test]
        public void should_throw_for_non_successful_api_response()
        {
            var response = CreateIndexerResponse("{}", HttpStatusCode.InternalServerError);

            Action act = () => Subject.GetParser().ParseResponse(response);

            act.Should().Throw<IndexerException>();
        }

        private void SetupResponses(string searchResponse, string detailsResponse, string recentResponse)
        {
            _requests.Clear();
            Mocker.GetMock<IIndexerHttpClient>()
                .Setup(o => o.ExecuteProxiedAsync(It.Is<HttpRequest>(v => v.Method == HttpMethod.Get), Subject.Definition))
                .Returns<HttpRequest, IndexerDefinition>((request, definition) =>
                {
                    var url = request.Url.FullUri;
                    _requests.Add(url);
                    var response = url.Contains("/app/search/releases?", StringComparison.Ordinal)
                        ? searchResponse
                        : url.Contains("/anime/torrents?", StringComparison.Ordinal)
                            ? recentResponse
                            : detailsResponse;
                    return Task.FromResult(new HttpResponse(request, JsonHeaders(), new CookieCollection(), response));
                });
        }

        private static HttpHeader JsonHeaders() => new() { { "Content-Type", "application/json" } };

        private static IndexerResponse CreateIndexerResponse(string content, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            var request = new IndexerRequest("https://aniliberty.top/api/v1/anime/releases/list?ids=100", HttpAccept.Json);
            return new IndexerResponse(request, new HttpResponse(request.HttpRequest, JsonHeaders(), new CookieCollection(), content, statusCode: statusCode));
        }

        private static IndexerResponse CreateSearchIndexerResponse(string content, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            var request = new IndexerRequest("https://aniliberty.top/api/v1/app/search/releases?query=Test%20Anime", HttpAccept.Json);
            return new IndexerResponse(request, new HttpResponse(request.HttpRequest, JsonHeaders(), new CookieCollection(), content, statusCode: statusCode));
        }

        private static IndexerResponse CreateRecentIndexerResponse(string content, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            var request = new IndexerRequest("https://aniliberty.top/api/v1/anime/torrents?limit=50", HttpAccept.Json);
            return new IndexerResponse(request, new HttpResponse(request.HttpRequest, JsonHeaders(), new CookieCollection(), content, statusCode: statusCode));
        }
    }
}
