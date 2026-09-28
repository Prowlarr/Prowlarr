using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Http;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Indexers.Definitions;
using NzbDrone.Core.Indexers.Settings;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IndexerTests.WolfMax4KTests
{
    [TestFixture]
    public class WolfMax4KFixture : CoreTest<WolfMax4K>
    {
        [SetUp]
        public void Setup()
        {
            Subject.Definition = new IndexerDefinition
            {
                Name = "WolfMax4K",
                Settings = new NoAuthTorrentBaseSettings
                {
                    BaseUrl = "https://wolfmax4k.com/"
                }
            };
        }

        private void GivenSearchResponse(string file)
        {
            var html = ReadAllText(file);

            Mocker.GetMock<IIndexerHttpClient>()
                .Setup(o => o.ExecuteProxiedAsync(It.Is<HttpRequest>(r => r.Method == HttpMethod.Get && r.Url.Path.StartsWith("/buscar")), Subject.Definition))
                .ReturnsAsync((HttpRequest r, IndexerDefinition _) => new HttpResponse(r, new HttpHeader { { "Content-Type", "text/html; charset=utf-8" } }, new CookieCollection(), html));
        }

        private void GivenExtraEpisodes()
        {
            var json = ReadAllText("Files/Indexers/WolfMax4K/episodes.json");

            Mocker.GetMock<IIndexerHttpClient>()
                .Setup(o => o.ExecuteProxied(It.Is<HttpRequest>(r => r.Url.Path == "/api/episodios"), Subject.Definition))
                .Returns((HttpRequest r, IndexerDefinition _) => new HttpResponse(r, new HttpHeader { { "Content-Type", "application/json" } }, new CookieCollection(), json));
        }

        private void GivenDownloadApi(string generateJson, string validateJson, HttpStatusCode validateStatus = HttpStatusCode.OK)
        {
            Mocker.GetMock<IIndexerHttpClient>()
                .Setup(o => o.ExecuteProxiedAsync(It.Is<HttpRequest>(r => r.Method == HttpMethod.Post && r.Url.Path == "/api/descargas"), Subject.Definition))
                .ReturnsAsync((HttpRequest r, IndexerDefinition _) =>
                {
                    var body = Encoding.UTF8.GetString(r.ContentData);
                    var isGenerate = body.Contains("\"generate\"");

                    return new HttpResponse(r, new HttpHeader { { "Content-Type", "application/json" } }, new CookieCollection(), isGenerate ? generateJson : validateJson, statusCode: isGenerate ? HttpStatusCode.OK : validateStatus);
                });
        }

        private static byte[] BuildTorrent()
        {
            var pieces = new string('a', 20);
            var bencoded = $"d8:announce26:udp://tracker.example:13374:infod6:lengthi1024e4:name8:test.mkv12:piece lengthi16384e6:pieces20:{pieces}ee";

            return Encoding.ASCII.GetBytes(bencoded);
        }

        [Test]
        public async Task should_parse_search_results()
        {
            GivenSearchResponse("Files/Indexers/WolfMax4K/search.html");
            GivenExtraEpisodes();

            var releases = (await Subject.Fetch(new BasicSearchCriteria { SearchTerm = "the boys" })).Releases;

            releases.Should().HaveCount(9);
            releases.Select(r => r.Title).Should().Equal(
                "The Boys of Ghost Town (2008) DVDRip VOSE",
                "The Boys S01E05-E08 HDTV SPANISH",
                "The Boys S01E02-E04 HDTV SPANISH",
                "The Boys S01E01 HDTV SPANISH",
                "The Boys S02E01E02 HDTV 1080p SPANISH",
                "Matrix (1999) 2160p SPANISH",
                "Matrix (1999) BluRay 1080p SPANISH",
                "Matrix (1999) BDRemux 1080p SPANISH",
                "Los chicos del coro: la historia HDTV 720p SPANISH");

            var movie = (TorrentInfo)releases.Single(r => r.Title == "Matrix (1999) 2160p SPANISH");
            movie.Guid.Should().Be("https://wolfmax4k.com/peliculas/368874");
            movie.InfoUrl.Should().Be("https://wolfmax4k.com/pelicula/b8r7m6");
            movie.DownloadUrl.Should().Be("https://wolfmax4k.com/api/descargas?tabla=peliculas&content_id=368874");
            movie.Size.Should().Be(60333553090);
            movie.PublishDate.Should().Be(new DateTime(2021, 6, 21, 0, 0, 0, DateTimeKind.Utc));
            movie.Year.Should().Be(1999);
            movie.Genres.Should().BeEquivalentTo("Acción", "Aventura");
            movie.Categories.Select(c => c.Id).Should().Contain(new[] { NewznabStandardCategory.Movies.Id, NewznabStandardCategory.MoviesUHD.Id });
            movie.Seeders.Should().Be(1);
            movie.DownloadVolumeFactor.Should().Be(0);
            movie.UploadVolumeFactor.Should().Be(1);

            var episode = (TorrentInfo)releases.Single(r => r.Title == "The Boys S02E01E02 HDTV 1080p SPANISH");
            episode.InfoUrl.Should().Be("https://wolfmax4k.com/serie/episodio/ht9aa1");
            episode.Categories.Select(c => c.Id).Should().Contain(new[] { NewznabStandardCategory.TV.Id, NewznabStandardCategory.TVHD.Id });
            episode.Size.Should().Be(3328599654);

            var extra = releases.Single(r => r.Title == "The Boys S01E01 HDTV SPANISH");
            extra.PublishDate.Should().Be(new DateTime(2019, 8, 13, 0, 0, 0, DateTimeKind.Utc));
            extra.Categories.Select(c => c.Id).Should().Contain(new[] { NewznabStandardCategory.TV.Id, NewznabStandardCategory.TVSD.Id });

            var documentary = releases.Single(r => r.Title.StartsWith("Los chicos del coro"));
            documentary.Categories.Select(c => c.Id).Should().Contain(NewznabStandardCategory.TVDocumentary.Id).And.NotContain(NewznabStandardCategory.Movies.Id);
        }

        [Test]
        public async Task should_return_no_results_for_empty_search()
        {
            GivenSearchResponse("Files/Indexers/WolfMax4K/empty.html");

            var releases = (await Subject.Fetch(new BasicSearchCriteria { SearchTerm = "zzqqxx" })).Releases;

            releases.Should().BeEmpty();
        }

        [Test]
        public void should_search_by_title_without_year()
        {
            var requests = Subject.GetRequestGenerator().GetSearchRequests(new MovieSearchCriteria { SearchTerm = "Matrix 1999" });

            requests.GetAllTiers().First().First().Url.FullUri.Should().Be("https://wolfmax4k.com/buscar?q=Matrix");
        }

        [Test]
        public void should_search_tv_by_show_name_only()
        {
            var requests = Subject.GetRequestGenerator().GetSearchRequests(new TvSearchCriteria { SearchTerm = "The Boys", Season = 1, Episode = "5" });

            requests.GetAllTiers().First().First().Url.FullUri.Should().Be("https://wolfmax4k.com/buscar?q=The%20Boys");
        }

        [Test]
        public void should_use_latest_page_for_rss()
        {
            var requests = Subject.GetRequestGenerator().GetSearchRequests(new BasicSearchCriteria());

            requests.GetAllTiers().First().First().Url.FullUri.Should().Be("https://wolfmax4k.com/ultimos");
        }

        [Test]
        public async Task should_solve_challenge_and_download_torrent()
        {
            var torrent = BuildTorrent();

            GivenDownloadApi(
                "{\"success\":true,\"challenge\":\"wolfmax4k-test-challenge\"}",
                "{\"success\":true,\"download_url\":\"/torrents/peliculas/Matrix-(4K)-(WolfMax4K)-(AbCd).torrent\"}");

            Mocker.GetMock<IIndexerHttpClient>()
                .Setup(o => o.ExecuteProxiedAsync(It.Is<HttpRequest>(r => r.Method == HttpMethod.Get && r.Url.Path.StartsWith("/torrents/")), Subject.Definition))
                .ReturnsAsync((HttpRequest r, IndexerDefinition _) => new HttpResponse(r, new HttpHeader { { "Content-Type", "application/x-bittorrent" } }, new CookieCollection(), torrent));

            var response = await Subject.Download(new Uri("https://wolfmax4k.com/api/descargas?tabla=peliculas&content_id=368874"));

            response.Data.Should().Equal(torrent);

            Mocker.GetMock<IIndexerHttpClient>()
                .Verify(o => o.ExecuteProxiedAsync(It.Is<HttpRequest>(r => r.Method == HttpMethod.Post && Encoding.UTF8.GetString(r.ContentData).Contains("\"nonce\":66")), Subject.Definition), Times.Once());
        }

        [Test]
        public void should_not_try_to_solve_captcha()
        {
            GivenDownloadApi(
                "{\"success\":true,\"challenge\":\"wolfmax4k-test-challenge\"}",
                "{\"success\":false,\"status\":\"captcha_required\",\"captcha_image\":\"data:image/png;base64,AAAA\",\"message\":\"Verifica que eres humano\"}");

            Assert.ThrowsAsync<ReleaseDownloadException>(() => Subject.Download(new Uri("https://wolfmax4k.com/api/descargas?tabla=peliculas&content_id=368874")));
        }

        [Test]
        public void should_report_download_limit()
        {
            GivenDownloadApi(
                "{\"success\":true,\"challenge\":\"wolfmax4k-test-challenge\"}",
                "{\"success\":false,\"status\":\"limit_exceeded\",\"wait_minutes\":45}",
                HttpStatusCode.TooManyRequests);

            var ex = Assert.ThrowsAsync<ReleaseDownloadException>(() => Subject.Download(new Uri("https://wolfmax4k.com/api/descargas?tabla=peliculas&content_id=368874")));

            ex.Message.Should().Contain("45");
        }

        [Test]
        public void should_mark_missing_content_as_unavailable()
        {
            Mocker.GetMock<IIndexerHttpClient>()
                .Setup(o => o.ExecuteProxiedAsync(It.Is<HttpRequest>(r => r.Method == HttpMethod.Post), Subject.Definition))
                .ReturnsAsync((HttpRequest r, IndexerDefinition _) => new HttpResponse(r, new HttpHeader(), new CookieCollection(), "{\"success\":false,\"error\":\"Contenido no encontrado\"}", statusCode: HttpStatusCode.NotFound));

            Assert.ThrowsAsync<ReleaseUnavailableException>(() => Subject.Download(new Uri("https://wolfmax4k.com/api/descargas?tabla=peliculas&content_id=1")));
        }

        [TestCase("wolfmax4k-test-challenge", 66)]
        [TestCase("0f1e2d3c4b5a69788796a5b4c3d2e1f00f1e2d3c4b5a69788796a5b4c3d2e1f0", 7042)]
        public void should_solve_proof_of_work_like_the_site(string challenge, long expected)
        {
            WolfMax4KProofOfWork.Solve(challenge).Should().Be(expected);
        }

        [TestCase("peliculas", "Puño de león", 2026, null, "HDRip", "Puño de león (2026) HDRip SPANISH")]
        [TestCase("peliculas", "Las catadoras del Hitler", 2025, null, "BluRay-1080p", "Las catadoras del Hitler (2025) BluRay 1080p SPANISH")]
        [TestCase("series", "Delirio mortal - 1ª Temporada [720p]", 0, "Episodio 1x03", "HDTV-720p", "Delirio mortal S01E03 HDTV 720p SPANISH")]
        [TestCase("series", "El problema final - 1ª Temporada [1080p]", 0, "Episodio 1x02", "4K", "El problema final S01E02 2160p SPANISH")]
        [TestCase("documentales", "Drag Race España", 0, "Episodio 2x01", "HDTV-720p", "Drag Race España S02E01 HDTV 720p SPANISH")]
        [TestCase("documentales", "Gemelos malvados   - 1ª Temporada [1080p]", 0, "Episodio 1x05 al 1x07", "HDTV-1080p", "Gemelos malvados S01E05-E07 HDTV 1080p SPANISH")]
        [TestCase("telenovelas", "En Tierra Lejana - 2ª Temporada [1080p]", 0, "Episodio 2x05", "HDTV-1080p", "En Tierra Lejana S02E05 HDTV 1080p SPANISH")]
        [TestCase("animaciones", "Stranger Things: Relatos del 85 - 2ª Temporada [1080p]", 0, "Episodio 2x08", "HDTV-1080p", "Stranger Things: Relatos del 85 S02E08 HDTV 1080p SPANISH")]
        public void should_build_parseable_titles(string tabla, string cardTitle, int year, string episodeLabel, string quality, string expected)
        {
            WolfMax4KTitleBuilder.Build(tabla, cardTitle, year, episodeLabel, quality).Title.Should().Be(expected);
        }
    }
}
