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
using NzbDrone.Core.Indexers.Definitions.Cardigann;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IndexerTests.LostFilmTests
{
    [TestFixture]
    public class LostFilmFixture : CoreTest<LostFilm>
    {
        private const string BaseUrl = "http://www.lostfilm.tv";

        [SetUp]
        public void Setup()
        {
            Subject.Definition = new IndexerDefinition
            {
                Name = "LostFilm",
                Settings = new LostFilmSettings
                {
                    BaseUrl = BaseUrl,
                    Username = "testuser",
                    Password = "testpass"
                }
            };
        }

        private void MockResponse(HttpMethod method, string path, string content, string contentType = "text/html")
        {
            Mocker.GetMock<IIndexerHttpClient>()
                .Setup(o => o.ExecuteProxiedAsync(It.Is<HttpRequest>(v => v.Method == method && v.Url.Path == path), Subject.Definition))
                .Returns<HttpRequest, IndexerDefinition>((r, d) =>
                    Task.FromResult(new HttpResponse(r, new HttpHeader { { "Content-Type", contentType } }, new CookieCollection(), content)));
        }

        private void MockResponse(HttpMethod method, string path, byte[] content, string contentType)
        {
            Mocker.GetMock<IIndexerHttpClient>()
                .Setup(o => o.ExecuteProxiedAsync(It.Is<HttpRequest>(v => v.Method == method && v.Url.Path == path), Subject.Definition))
                .Returns<HttpRequest, IndexerDefinition>((r, d) =>
                    Task.FromResult(new HttpResponse(r, new HttpHeader { { "Content-Type", contentType } }, new CookieCollection(), content)));
        }

        private void MockAllResponses()
        {
            // Fallback to an empty page so unmatched requests do not fail the test.
            // Must be registered first: in Moq the last matching setup wins.
            Mocker.GetMock<IIndexerHttpClient>()
                .Setup(o => o.ExecuteProxiedAsync(It.IsAny<HttpRequest>(), Subject.Definition))
                .Returns<HttpRequest, IndexerDefinition>((r, d) =>
                    Task.FromResult(new HttpResponse(r, new HttpHeader { { "Content-Type", "text/html" } }, new CookieCollection(), "<html><body></body></html>")));

            MockResponse(HttpMethod.Post, "/ajaxik.php", ReadAllText(@"Files/Indexers/LostFilm/search_breakingbad.json"), "application/json");
            MockResponse(HttpMethod.Get, "/series/Breaking_Bad/season_5/episode_16", ReadAllText(@"Files/Indexers/LostFilm/episode_auth.html"));
            MockResponse(HttpMethod.Get, "/movies/El_Camino_A_Breaking_Bad_Movie/season_5/episode_16", "<html><body></body></html>");
            MockResponse(HttpMethod.Get, "/v_search.php", ReadAllText(@"Files/Indexers/LostFilm/vsearch_bb.html"));
            MockResponse(HttpMethod.Get, "/V/", ReadAllText(@"Files/Indexers/LostFilm/tracker_bb.html"));
        }

        [Test]
        public async Task should_parse_episode_releases_from_LostFilm()
        {
            MockAllResponses();

            var result = await Subject.Fetch(new TvSearchCriteria { SearchTerm = "breaking bad", Season = 5, Episode = "16", Categories = new[] { 5000 } });

            result.Queries.Should().HaveCount(1);
            result.Queries[0].Releases.Should().HaveCount(3);

            var releases = result.Releases;

            releases.Should().HaveCount(3);
            releases.Should().OnlyContain(c => c.GetType() == typeof(TorrentInfo));

            var torrentInfo = releases.First() as TorrentInfo;

            torrentInfo.Title.Should().Be("Breaking Bad - S5E16 - rus WEBDL (LostFilm)");
            torrentInfo.DownloadProtocol.Should().Be(DownloadProtocol.Torrent);
            torrentInfo.DownloadUrl.Should().StartWith("https://n.tracktor.site/td.php?s=");
            torrentInfo.InfoUrl.Should().Be($"{BaseUrl}/series/Breaking_Bad/season_5/episode_16");
            torrentInfo.CommentUrl.Should().BeNullOrEmpty();
            torrentInfo.Indexer.Should().Be(Subject.Definition.Name);
            torrentInfo.PublishDate.Should().Be(DateTime.Parse("2013-09-29"));
            torrentInfo.Size.Should().Be(11800422645);
            torrentInfo.Seeders.Should().Be(1);
            torrentInfo.Peers.Should().Be(2);
            torrentInfo.DownloadVolumeFactor.Should().Be(0);
            torrentInfo.UploadVolumeFactor.Should().Be(1);
            torrentInfo.Categories.Should().ContainSingle();
        }

        [Test]
        public void should_return_captcha_from_checkCaptcha_action()
        {
            MockResponse(HttpMethod.Post, "/ajaxik.php", "{\"success\":true,\"result\":\"ok\"}", "application/json");
            MockResponse(HttpMethod.Get, "/login", ReadAllText(@"Files/Indexers/LostFilm/login.html"));
            MockResponse(HttpMethod.Get, "/simple_captcha.php", new byte[] { 0x89, 0x50, 0x4E, 0x47 }, "image/jpeg");

            var result = Subject.RequestAction("checkCaptcha", new Dictionary<string, string>());

            var captchaRequest = result.GetType().GetProperty("captchaRequest").GetValue(result) as Captcha;

            captchaRequest.Should().NotBeNull();
            captchaRequest.ContentType.Should().Be("image/jpeg");
            captchaRequest.ImageData.Should().Equal(0x89, 0x50, 0x4E, 0x47);
        }

        [Test]
        public async Task should_find_releases_for_movie_search()
        {
            MockResponse(HttpMethod.Post, "/ajaxik.php", ReadAllText(@"Files/Indexers/LostFilm/search_avatar.json"), "application/json");
            MockResponse(HttpMethod.Get, "/movies/Avatar_Aang_The_Last_Airbender/seasons", ReadAllText(@"Files/Indexers/LostFilm/episode_auth.html"));
            MockResponse(HttpMethod.Get, "/movies/Avatar_Aang_The_Last_Airbender", ReadAllText(@"Files/Indexers/LostFilm/episode_auth.html"));
            MockResponse(HttpMethod.Get, "/v_search.php", ReadAllText(@"Files/Indexers/LostFilm/vsearch_bb.html"));
            MockResponse(HttpMethod.Get, "/V/", ReadAllText(@"Files/Indexers/LostFilm/tracker_bb.html"));

            var result = await Subject.Fetch(new MovieSearchCriteria { SearchTerm = "avatar aang", Categories = new[] { 2000 } });

            result.Releases.Should().HaveCount(3);
            result.Releases.Should().OnlyContain(r => r.InfoUrl == $"{BaseUrl}/movies/Avatar_Aang_The_Last_Airbender");
        }

        [Test]
        public void should_search_with_basic_search_criteria()
        {
            var chain = Subject.GetRequestGenerator().GetSearchRequests(new BasicSearchCriteria { SearchTerm = "breaking bad" });

            var paths = chain.GetAllTiers().SelectMany(x => x).Select(x => x.HttpRequest.Url.Path).ToList();

            paths.Should().Contain("/ajaxik.php");
            paths.Should().NotContain("/new");
        }

        [Test]
        public void should_return_rss_for_empty_basic_search_criteria()
        {
            var chain = Subject.GetRequestGenerator().GetSearchRequests(new BasicSearchCriteria());

            var paths = chain.GetAllTiers().SelectMany(x => x).Select(x => x.HttpRequest.Url.Path).ToList();

            paths.Should().Contain("/new");
            paths.Should().NotContain("/ajaxik.php");
        }
    }
}
