using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Http;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Indexers.Definitions;
using NzbDrone.Core.Indexers.Settings;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IndexerTests.DonTorrentTests
{
    [TestFixture]
    public class DonTorrentFixture : CoreTest<DonTorrent>
    {
        private const string BaseUrl = "https://dontorrent.moi/";

        [SetUp]
        public void Setup()
        {
            Mocker.SetConstant<ICacheManager>(Mocker.Resolve<CacheManager>());

            Subject.Definition = new IndexerDefinition
            {
                Name = "DonTorrent",
                Settings = new NoAuthTorrentBaseSettings
                {
                    BaseUrl = BaseUrl
                }
            };
        }

        private static HttpResponse Html(HttpRequest request, string content)
        {
            return new HttpResponse(request, new HttpHeader { { "Content-Type", "text/html; charset=utf-8" } }, new CookieCollection(), content);
        }

        private void GivenSite()
        {
            var files = new[]
            {
                ("/pelicula/1078/", "pelicula-1078.html"),
                ("/pelicula/23404/", "pelicula-23404.html"),
                ("/serie/62314/", "serie-62314.html"),
                ("/documental/2063/", "documental-2063.html")
            };

            var search = ReadAllText("Files/Indexers/DonTorrent/search.html");
            var latest = ReadAllText("Files/Indexers/DonTorrent/ultimos.html");

            Mocker.GetMock<IIndexerHttpClient>()
                .Setup(o => o.ExecuteProxiedAsync(It.Is<HttpRequest>(r => r.Url.Path == "/buscar" && r.Method == HttpMethod.Post), Subject.Definition))
                .ReturnsAsync((HttpRequest r, IndexerDefinition _) => Html(r, search));

            Mocker.GetMock<IIndexerHttpClient>()
                .Setup(o => o.ExecuteProxiedAsync(It.Is<HttpRequest>(r => r.Url.Path == "/ultimos"), Subject.Definition))
                .ReturnsAsync((HttpRequest r, IndexerDefinition _) => Html(r, latest));

            foreach (var (path, file) in files)
            {
                var content = ReadAllText($"Files/Indexers/DonTorrent/{file}");

                Mocker.GetMock<IIndexerHttpClient>()
                    .Setup(o => o.ExecuteProxied(It.Is<HttpRequest>(r => r.Url.Path.StartsWith(path)), Subject.Definition))
                    .Returns((HttpRequest r, IndexerDefinition _) => Html(r, content));
            }
        }

        [Test]
        public async Task should_parse_search_results_with_details()
        {
            GivenSite();

            var releases = (await Subject.Fetch(new BasicSearchCriteria { SearchTerm = "the boys" })).Releases;

            releases.Select(r => r.Title).Should().Equal(
                "The Boys of Ghost Town (2008) DVDRip VOSE",
                "The Boys S01E01 HDTV SPANISH",
                "The Boys S01E02-E04 HDTV SPANISH",
                "The Boys S01E05-E08 HDTV SPANISH",
                "Matrix (1999) 2160p SPANISH",
                "Desmontando el cosmos S02E01 HDTV 720p SPANISH",
                "Desmontando el cosmos S02E02 HDTV 720p SPANISH");

            var movie = (TorrentInfo)releases.Single(r => r.Title == "Matrix (1999) 2160p SPANISH");
            movie.Guid.Should().Be("https://dontorrent.moi/peliculas/23404");
            movie.InfoUrl.Should().Be("https://dontorrent.moi/pelicula/23404/Matrix-4K");
            movie.DownloadUrl.Should().Be("https://dontorrent.moi/api_validate_pow.php?tabla=peliculas&content_id=23404");
            movie.Year.Should().Be(1999);
            movie.Size.Should().BeGreaterThan(0);
            movie.Categories.Select(c => c.Id).Should().Contain(new[] { NewznabStandardCategory.Movies.Id, NewznabStandardCategory.MoviesUHD.Id });

            var episodes = releases.Single(r => r.Title == "The Boys S01E05-E08 HDTV SPANISH");
            episodes.Guid.Should().Be("https://dontorrent.moi/series/62317");
            episodes.PublishDate.Should().Be(new DateTime(2019, 8, 13, 0, 0, 0, DateTimeKind.Utc));
            episodes.Size.Should().Be(releases.Single(r => r.Title == "The Boys S01E01 HDTV SPANISH").Size * 4);
            episodes.Categories.Select(c => c.Id).Should().Contain(new[] { NewznabStandardCategory.TV.Id, NewznabStandardCategory.TVSD.Id });

            var documentary = releases.Single(r => r.Title == "Desmontando el cosmos S02E02 HDTV 720p SPANISH");
            documentary.DownloadUrl.Should().Be("https://dontorrent.moi/api_validate_pow.php?tabla=documentales&content_id=2065");
            documentary.Categories.Select(c => c.Id).Should().Contain(NewznabStandardCategory.TVDocumentary.Id);
        }

        [Test]
        public async Task should_only_return_movies_for_movie_search()
        {
            GivenSite();

            var releases = (await Subject.Fetch(new MovieSearchCriteria { SearchTerm = "the boys" })).Releases;

            releases.Select(r => r.Title).Should().Equal("The Boys of Ghost Town (2008) DVDRip VOSE", "Matrix (1999) 2160p SPANISH");

            Mocker.GetMock<IIndexerHttpClient>()
                .Verify(o => o.ExecuteProxied(It.Is<HttpRequest>(r => r.Url.Path.StartsWith("/serie/")), It.IsAny<IndexerDefinition>()), Times.Never());
        }

        [Test]
        public async Task should_parse_latest_page()
        {
            GivenSite();

            var releases = (await Subject.Fetch(new BasicSearchCriteria())).Releases;

            releases.Select(r => r.Title).Should().Equal("Matrix (1999) 2160p SPANISH", "The Boys S01E01 HDTV SPANISH");
            releases.First().PublishDate.Should().Be(new DateTime(2026, 9, 28, 0, 0, 0, DateTimeKind.Utc));
        }

        [Test]
        public void should_post_search_form()
        {
            var request = Subject.GetRequestGenerator().GetSearchRequests(new MovieSearchCriteria { SearchTerm = "Matrix 1999" }).GetAllTiers().First().First();

            request.HttpRequest.Method.Should().Be(HttpMethod.Post);
            request.Url.FullUri.Should().Be("https://dontorrent.moi/buscar");
            Encoding.UTF8.GetString(request.HttpRequest.ContentData).Should().Be("valor=Matrix&Buscar=Buscar");
        }

        [Test]
        public async Task should_solve_challenge_and_download_torrent()
        {
            var torrent = Encoding.ASCII.GetBytes($"d8:announce26:udp://tracker.example:13374:infod6:lengthi1024e4:name8:test.mkv12:piece lengthi16384e6:pieces20:{new string('a', 20)}ee");

            Mocker.GetMock<IIndexerHttpClient>()
                .Setup(o => o.ExecuteProxiedAsync(It.Is<HttpRequest>(r => r.Method == HttpMethod.Post && r.Url.Path == "/api_validate_pow.php"), Subject.Definition))
                .ReturnsAsync((HttpRequest r, IndexerDefinition _) =>
                {
                    var body = Encoding.UTF8.GetString(r.ContentData);
                    var json = body.Contains("\"generate\"")
                        ? "{\"success\":true,\"challenge\":\"wolfmax4k-test-challenge\"}"
                        : "{\"success\":true,\"download_url\":\"https://dontorrent.moi/torrents/series/The-Boys-1-1-HDTV.torrent\"}";

                    return new HttpResponse(r, new HttpHeader { { "Content-Type", "application/json" } }, new CookieCollection(), json);
                });

            Mocker.GetMock<IIndexerHttpClient>()
                .Setup(o => o.ExecuteProxiedAsync(It.Is<HttpRequest>(r => r.Method == HttpMethod.Get && r.Url.Path.StartsWith("/torrents/")), Subject.Definition))
                .ReturnsAsync((HttpRequest r, IndexerDefinition _) => new HttpResponse(r, new HttpHeader { { "Content-Type", "application/x-bittorrent" } }, new CookieCollection(), torrent));

            var response = await Subject.Download(new Uri("https://dontorrent.moi/api_validate_pow.php?tabla=series&content_id=62315"));

            response.Data.Should().Equal(torrent);

            Mocker.GetMock<IIndexerHttpClient>()
                .Verify(o => o.ExecuteProxiedAsync(It.Is<HttpRequest>(r => r.Method == HttpMethod.Post && Encoding.UTF8.GetString(r.ContentData).Contains("\"nonce\":66")), Subject.Definition), Times.Once());
        }

        [Test]
        public void should_not_try_to_solve_captcha()
        {
            Mocker.GetMock<IIndexerHttpClient>()
                .Setup(o => o.ExecuteProxiedAsync(It.Is<HttpRequest>(r => r.Method == HttpMethod.Post), Subject.Definition))
                .ReturnsAsync((HttpRequest r, IndexerDefinition _) =>
                {
                    var body = Encoding.UTF8.GetString(r.ContentData);
                    var json = body.Contains("\"generate\"")
                        ? "{\"success\":true,\"challenge\":\"wolfmax4k-test-challenge\"}"
                        : "{\"success\":false,\"status\":\"captcha_required\",\"captcha_image\":\"x\"}";

                    return new HttpResponse(r, new HttpHeader(), new CookieCollection(), json);
                });

            Assert.ThrowsAsync<ReleaseDownloadException>(() => Subject.Download(new Uri("https://dontorrent.moi/api_validate_pow.php?tabla=peliculas&content_id=970")));
        }

        [TestCase("peliculas", "Matrix.", 1999, null, "HDRip", "Matrix (1999) HDRip SPANISH")]
        [TestCase("peliculas", "Matrix Resurrections [4K]", 2021, null, "4K", "Matrix Resurrections (2021) 2160p SPANISH")]
        [TestCase("series", "The Boys - 1ª Temporada [720p]", 0, "1x02 al 04.", "HDTV-720p", "The Boys S01E02-E04 HDTV 720p SPANISH")]
        [TestCase("series", "Historia de dos ciudades - 1ª Temporada [1080p]", 0, "1x03 -", null, "Historia de dos ciudades S01E03 1080p SPANISH")]
        [TestCase("documentales", "Cosmos: Otros mundos [720p].", 0, "1x01 -", "HDTV-720p", "Cosmos: Otros mundos S01E01 HDTV 720p SPANISH")]
        public void should_build_parseable_titles(string tabla, string title, int year, string episode, string quality, string expected)
        {
            DonTorrentTitleBuilder.Build(tabla, title, year, episode, quality).Title.Should().Be(expected);
        }
    }
}
