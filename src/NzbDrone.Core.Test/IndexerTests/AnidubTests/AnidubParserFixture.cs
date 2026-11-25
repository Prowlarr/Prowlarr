using System;
using System.Linq;
using System.Net;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Http;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Indexers.Definitions;
using NzbDrone.Core.Indexers.Settings;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IndexerTests.AnidubTests
{
    [TestFixture]
    public class AnidubParserFixture : CoreTest<Anidub>
    {
        private const string SearchPageHtml = @"<html><body><div class=""searchitem"">
            <h3><a href=""https://tr.anidub.com/anime_tv/anime_ongoing/11775-provozhayuschaya-v-posledniy-put-friren-sousou-no-frieren-04-iz-xx.html"">Провожающая в последний путь Фрирен / Sousou no Frieren [28 из 28]</a></h3>
            </div></body></html>";

        private AnidubParser _parser;

        [SetUp]
        public void Setup()
        {
            var settings = new UserPassTorrentBaseSettings { BaseUrl = "https://tr.anidub.com/" };

            var categories = new IndexerCapabilitiesCategories();
            categories.AddCategoryMapping(2, NewznabStandardCategory.TVAnime, "Аниме TV");
            categories.AddCategoryMapping(10, NewznabStandardCategory.TVAnime, "Аниме TV / Аниме Ongoing");

            var detailPage = ReadAllText(@"Files/Indexers/Anidub/frieren-detail.html");

            Mocker.GetMock<IIndexerHttpClient>()
                .Setup(o => o.ExecuteProxied(It.IsAny<HttpRequest>(), It.IsAny<NzbDrone.Core.ThingiProvider.ProviderDefinition>()))
                .Returns<HttpRequest, NzbDrone.Core.ThingiProvider.ProviderDefinition>((r, d) => new HttpResponse(r, new HttpHeader(), new CookieCollection(), detailPage));

            _parser = new AnidubParser(new IndexerDefinition { Name = "Anidub" }, settings, categories, TimeSpan.Zero, Mocker.GetMock<IIndexerHttpClient>().Object, NLog.LogManager.GetLogger("test"));
        }

        [Test]
        public void should_parse_quality_and_torrent_id_from_release_page()
        {
            var searchRequest = new IndexerRequest("https://tr.anidub.com/index.php?do=search", HttpAccept.Html);
            var searchResponse = new IndexerResponse(searchRequest, new HttpResponse(searchRequest.HttpRequest, new HttpHeader(), new CookieCollection(), SearchPageHtml));

            var releases = _parser.ParseResponse(searchResponse);

            releases.Should().HaveCount(1);

            var release = releases.Single() as TorrentInfo;

            release.Title.Should().Be("Провожающая в последний путь Фрирен / Sousou no Frieren [28 из 28] [HDTV 1080p]");
            release.DownloadUrl.Should().Be("https://tr.anidub.com/engine/download.php?id=38377");
            release.Resolution.Should().Be("HDTV 1080p");
            release.Seeders.Should().Be(12);
        }
    }
}
