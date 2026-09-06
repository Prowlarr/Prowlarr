using System;
using System.Linq;
using System.Net;
using AngleSharp.Html.Parser;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Common.Http;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Indexers.Definitions;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IndexerTests
{
    [TestFixture]
    public class DonTorrentHelperFixture : CoreTest
    {
        [TestCase("Marco.Polo.2014", "Marco Polo")]
        [TestCase("Pelicula español 2020", "Pelicula")]
        [TestCase("Serie castellano 2019 ", "Serie")]
        [TestCase("Some-Show (2021) spa", "Some Show 2021")]
        public void should_normalize_search_term(string input, string expected)
        {
            DonTorrentHelper.NormalizeSearchTerm(input).Should().Be(expected);
        }

        [Test]
        public void should_parse_series_title_with_range()
        {
            var title = "The Mandalorian - 1ª Temporada [720p]";
            var episodeTitle = "1x04 - 05.";

            var result = DonTorrentHelper.ParseSeriesTitle(title, episodeTitle);

            result.Should().Be("The Mandalorian S01E04-E05 SPANISH 720p x264");
        }

        [Test]
        public void should_parse_series_title_with_language_and_codec()
        {
            var title = "Show ES-EN - 1ª Temporada [1080p] HDTV x265";
            var episodeTitle = "1x02";

            var result = DonTorrentHelper.ParseSeriesTitle(title, episodeTitle);

            result.Should().Be("Show ES-EN S01E02 SPANISH ENGLISH 1080p HDTV x265");
        }

        [TestCase("Show S01E01-E03", 3)]
        [TestCase("Show S01E02", 1)]
        [TestCase("Show S01E10-E12", 3)]
        [TestCase("Show S02E100", 1)]
        [TestCase("Show", 0)]
        public void should_count_episodes_from_title(string title, int expected)
        {
            DonTorrentHelper.GetEpisodeCountFromTitle(title).Should().Be(expected);
        }

        [Test]
        public void should_generate_release()
        {
            var categories = new IndexerCapabilitiesCategories();
            categories.AddCategoryMapping("pelicula", NewznabStandardCategory.Movies, "Pelicula");
            categories.AddCategoryMapping("peliculahd", NewznabStandardCategory.MoviesHD, "Peliculas HD");

            var settings = new DonTorrentSettings { BaseUrl = "https://todotorrents.org/" };
            var definition = new IndexerDefinition { Name = "DonTorrent", Settings = settings };
            var parser = new DonTorrentParser(definition, settings, categories, TimeSpan.Zero, null);

            var publishDate = new DateTime(2024, 01, 02, 03, 04, 05, DateTimeKind.Utc);
            var release = parser.GenerateRelease("Title", "https://info", "https://download", "pelicula", publishDate, 1234);

            release.Should().NotBeNull();
            release.Title.Should().Be("Title");
            release.InfoUrl.Should().Be("https://info");
            release.DownloadUrl.Should().Be("https://download");
            release.PublishDate.Should().Be(publishDate);
            release.Size.Should().Be(1234);
            release.Categories.Should().Contain(NewznabStandardCategory.Movies);

            var torrent = release as TorrentInfo;
            torrent.Should().NotBeNull();
            torrent.Seeders.Should().Be(1);
            torrent.Peers.Should().Be(2);
            torrent.DownloadVolumeFactor.Should().Be(0);
            torrent.UploadVolumeFactor.Should().Be(1);
        }

        [Test]
        public void should_parse_movie_release_from_document()
        {
            var html = @"
<html>
  <body>
    <div class=""d-inline-block ml-2"">
      <p>Año: <a>2021</a></p>
    </div>
    <div class=""descargar"">
      <div class=""card"">
        <div class=""card-body"">
          <div class=""text-center"">
            <div class=""d-inline-block""><p>Fecha: 2021-01-01</p></div>
            <div class=""d-inline-block""><p>1.5 GB</p></div>
          </div>
        </div>
      </div>
    </div>
  </body>
</html>";

            var doc = new HtmlParser().ParseDocument(html);
            var categories = new IndexerCapabilitiesCategories();
            categories.AddCategoryMapping("pelicula", NewznabStandardCategory.Movies, "Pelicula");
            categories.AddCategoryMapping("peliculahd", NewznabStandardCategory.MoviesHD, "Peliculas HD");

            var settings = new DonTorrentSettings { BaseUrl = "https://todotorrents.org/" };
            var definition = new IndexerDefinition { Name = "DonTorrent", Settings = settings };
            var parser = new DonTorrentParser(definition, settings, categories, TimeSpan.Zero, null);

            var release = parser.ParseMovieReleaseFromDocument("/descargar-peliculas", "Movie", "(HDRip-1080p)", doc);

            release.Should().NotBeNull();
            release.Title.Should().Be("Movie 2021 SPANISH BDRip 1080p");
            release.Size.Should().Be(ParseUtil.GetBytes("1.5 GB"));
            release.Categories.Should().Contain(NewznabStandardCategory.MoviesHD);
        }

        [Test]
        public void should_parse_newest_section_list_and_limit_results()
        {
            var html = @"
<html>
  <body>
    <div class='seccion' id='ultimos_torrents'>
      <div class='card'>
        <div class='card-body'>
          <div style='padding:10px;'>
            <div class='h5 text-dark'>PELICULAS:</div>
            <span class='text-muted'>2026-02-09</span>
            <a href='pelicula/30153/Movie-One' class='text-primary'>Movie One</a>
            <span class='text-muted'>(DVDRip)</span><br>
            <span class='text-muted'>2026-02-08</span>
            <a href='pelicula/30150/Movie-Two-4K' class='text-primary'>Movie Two [4K]</a>
            <span class='text-muted'>(4K)</span><br><br>
            <div class='h5 text-dark'>SERIES:</div>
            <span class='text-muted'>2026-02-11</span>
            <a href='serie/124023/124026/Show-1-Temporada-720p' class='text-primary'>Show - 1ª Temporada [720p]: 1x04</a><br>
            <span class='text-muted'>2026-02-11</span>
            <a href='serie/124024/124025/Show-1-Temporada' class='text-primary'>Show - 1ª Temporada: 1x04</a><br>
          </div>
        </div>
      </div>
    </div>
  </body>
</html>";

            var categories = new IndexerCapabilitiesCategories();
            categories.AddCategoryMapping("pelicula", NewznabStandardCategory.Movies, "Pelicula");
            categories.AddCategoryMapping("peliculahd", NewznabStandardCategory.MoviesHD, "Peliculas HD");
            categories.AddCategoryMapping("pelicula4k", NewznabStandardCategory.MoviesUHD, "Peliculas 4K");
            categories.AddCategoryMapping("serie", NewznabStandardCategory.TVSD, "Serie");
            categories.AddCategoryMapping("seriehd", NewznabStandardCategory.TVHD, "Serie HD");

            var settings = new DonTorrentSettings { BaseUrl = "https://todotorrents.org/" };
            var definition = new IndexerDefinition { Name = "DonTorrent", Settings = settings };
            var parser = new DonTorrentParser(definition, settings, categories, TimeSpan.Zero, null);

            var request = new HttpRequest("https://todotorrents.org/ultimos");
            var httpResponse = new HttpResponse(request, new HttpHeader(), new CookieCollection(), html, statusCode: HttpStatusCode.OK);
            var response = new IndexerResponse(new IndexerRequest(request), httpResponse);

            var releases = parser.ParseResponse(response);

            releases.Should().NotBeNull();
            releases.Count.Should().Be(4);
            releases.Select(r => r.Title).Should().Contain(t => t.Contains("Movie One"));
            releases.Select(r => r.Title).Should().Contain(t => t.Contains("Movie Two"));
            releases.Select(r => r.Title).Should().Contain(t => t.Contains("Show"));
            releases.SelectMany(r => r.Categories).Should().Contain(NewznabStandardCategory.MoviesUHD);
        }
    }
}
