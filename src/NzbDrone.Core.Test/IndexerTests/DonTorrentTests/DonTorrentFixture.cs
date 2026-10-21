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
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IndexerTests.DonTorrentTests
{
    [TestFixture]
    public class DonTorrentFixture : CoreTest<DonTorrent>
    {
        private const string BaseUrl = "https://dontorrent.meme/";

        [SetUp]
        public void Setup()
        {
            Mocker.SetConstant<ICacheManager>(Mocker.Resolve<CacheManager>());

            Subject.Definition = new IndexerDefinition
            {
                Name = "DonTorrent",
                Settings = new DonTorrentSettings
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
                "The Boys of Ghost Town (2008) DVDRip VOSE [Clave WWW.PCTNEW ORG cocineros]",
                "The Boys S01E01 HDTV SPANISH",
                "The Boys S01E02-E04 HDTV SPANISH [Clave WWW.PCTNEW ORG malotes]",
                "The Boys S01E05-E08 HDTV SPANISH",
                "Matrix (1999) 2160p SPANISH",
                "Desmontando el cosmos S02E01 HDTV 720p SPANISH",
                "Desmontando el cosmos S02E02 HDTV 720p SPANISH");

            var movie = (TorrentInfo)releases.Single(r => r.Title == "Matrix (1999) 2160p SPANISH");
            movie.Guid.Should().Be("DonTorrent-peliculas-23404");
            movie.InfoUrl.Should().Be("https://dontorrent.meme/pelicula/23404/Matrix-4K");
            movie.DownloadUrl.Should().Be("https://dontorrent.meme/api_validate_pow.php?tabla=peliculas&content_id=23404");
            movie.Year.Should().Be(1999);
            movie.Size.Should().BeGreaterThan(0);
            movie.Categories.Select(c => c.Id).Should().Contain(new[] { NewznabStandardCategory.Movies.Id, NewznabStandardCategory.MoviesUHD.Id });

            var episodes = releases.Single(r => r.Title == "The Boys S01E05-E08 HDTV SPANISH");
            episodes.Guid.Should().Be("DonTorrent-series-62317");
            episodes.PublishDate.Should().Be(new DateTime(2019, 8, 13, 0, 0, 0, DateTimeKind.Utc));
            episodes.Size.Should().Be(releases.Single(r => r.Title == "The Boys S01E01 HDTV SPANISH").Size * 4);
            episodes.Categories.Select(c => c.Id).Should().Contain(new[] { NewznabStandardCategory.TV.Id, NewznabStandardCategory.TVSD.Id });

            var documentary = releases.Single(r => r.Title == "Desmontando el cosmos S02E02 HDTV 720p SPANISH");
            documentary.DownloadUrl.Should().Be("https://dontorrent.meme/api_validate_pow.php?tabla=documentales&content_id=2065");
            documentary.Categories.Select(c => c.Id).Should().Contain(NewznabStandardCategory.TVDocumentary.Id);
        }

        [Test]
        public async Task should_only_return_movies_for_movie_search()
        {
            GivenSite();

            var releases = (await Subject.Fetch(new MovieSearchCriteria { SearchTerm = "the boys" })).Releases;

            releases.Select(r => r.Title).Should().Equal("The Boys of Ghost Town (2008) DVDRip VOSE [Clave WWW.PCTNEW ORG cocineros]", "Matrix (1999) 2160p SPANISH");

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
            request.Url.FullUri.Should().Be("https://dontorrent.meme/buscar");
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
                        : "{\"success\":true,\"download_url\":\"https://dontorrent.meme/torrents/series/The-Boys-1-1-HDTV.torrent\"}";

                    return new HttpResponse(r, new HttpHeader { { "Content-Type", "application/json" } }, new CookieCollection(), json);
                });

            Mocker.GetMock<IIndexerHttpClient>()
                .Setup(o => o.ExecuteProxiedAsync(It.Is<HttpRequest>(r => r.Method == HttpMethod.Get && r.Url.Path.StartsWith("/torrents/")), Subject.Definition))
                .ReturnsAsync((HttpRequest r, IndexerDefinition _) => new HttpResponse(r, new HttpHeader { { "Content-Type", "application/x-bittorrent" } }, new CookieCollection(), torrent));

            var response = await Subject.Download(new Uri("https://dontorrent.meme/api_validate_pow.php?tabla=series&content_id=62315"));

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

            Assert.ThrowsAsync<ReleaseDownloadException>(() => Subject.Download(new Uri("https://dontorrent.meme/api_validate_pow.php?tabla=peliculas&content_id=970")));
        }

        [Test]
        public async Task should_carry_archive_passwords()
        {
            GivenSite();

            var releases = (await Subject.Fetch(new BasicSearchCriteria { SearchTerm = "the boys" })).Releases;

            var episodes = releases.Single(r => r.Title.StartsWith("The Boys S01E02-E04"));
            episodes.Description.Should().Be("Contraseña / password: WWW.PCTNEW ORG malotes");
            episodes.DownloadUrl.Should().Be("https://dontorrent.meme/api_validate_pow.php?tabla=series&content_id=62316&clave=WWW.PCTNEW%20ORG%20malotes");

            var free = releases.Single(r => r.Title == "The Boys S01E01 HDTV SPANISH");
            free.Description.Should().BeNull();
            free.DownloadUrl.Should().NotContain("clave");
        }

        [Test]
        public void should_add_password_as_torrent_comment_without_changing_the_info_hash()
        {
            var torrent = Encoding.ASCII.GetBytes($"d8:announce26:udp://tracker.example:13374:infod6:lengthi1024e4:name8:test.mkv12:piece lengthi16384e6:pieces20:{new string('a', 20)}ee");

            var modified = DonTorrentTorrentComment.AddPassword(torrent, "WWW.PCTNEW ORG malotes");

            var original = MonoTorrent.Torrent.Load(torrent);
            var result = MonoTorrent.Torrent.Load(modified);

            result.Comment.Should().Be("Contraseña / password: WWW.PCTNEW ORG malotes");
            result.InfoHashes.V1OrV2.ToHex().Should().Be(original.InfoHashes.V1OrV2.ToHex());
        }

        [Test]
        public void should_use_alternative_domain_when_set()
        {
            ((DonTorrentSettings)Subject.Definition.Settings).AlternativeDomain = "dontorrent.manual";

            Subject.GetBaseUrl().Should().Be("https://dontorrent.manual/");
            Subject.GetRequestGenerator().GetSearchRequests(new BasicSearchCriteria()).GetAllTiers().First().First().Url.Host.Should().Be("dontorrent.manual");
        }

        [Test]
        public void should_read_current_domain_from_domain_list()
        {
            DonTorrent.ParseDomainDirectory(ReadAllText("Files/Indexers/DonTorrent/dominios.html")).Should().Be("https://dontorrent.newtld/");
        }

        [Test]
        public async Task should_follow_redirect_to_new_domain()
        {
            GivenSite();

            Mocker.GetMock<IIndexerHttpClient>()
                .Setup(o => o.ExecuteProxiedAsync(It.Is<HttpRequest>(r => r.Url.Host == "dontorrent.meme" && r.Url.Path == "/ultimos"), Subject.Definition))
                .ReturnsAsync((HttpRequest r, IndexerDefinition _) => new HttpResponse(r, new HttpHeader { { "Location", "https://dontorrent.newtld/ultimos" } }, new CookieCollection(), string.Empty, statusCode: HttpStatusCode.MovedPermanently));

            var latest = ReadAllText("Files/Indexers/DonTorrent/ultimos.html");

            Mocker.GetMock<IIndexerHttpClient>()
                .Setup(o => o.ExecuteProxiedAsync(It.Is<HttpRequest>(r => r.Url.Host == "dontorrent.newtld" && r.Url.Path == "/ultimos"), Subject.Definition))
                .ReturnsAsync((HttpRequest r, IndexerDefinition _) => Html(r, latest));

            var releases = (await Subject.Fetch(new BasicSearchCriteria())).Releases;

            releases.Should().NotBeEmpty();
            Subject.GetBaseUrl().Should().Be("https://dontorrent.newtld/");
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
