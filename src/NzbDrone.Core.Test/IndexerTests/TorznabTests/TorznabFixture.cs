using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Http;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Indexers.Newznab;
using NzbDrone.Core.Indexers.Torznab;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IndexerTests.TorznabTests
{
    [TestFixture]
    public class TorznabFixture : CoreTest<Torznab>
    {
        private IndexerCapabilities _caps;

        [SetUp]
        public void Setup()
        {
            Subject.Definition = new IndexerDefinition()
            {
                Name = "Torznab",
                Settings = new TorznabSettings()
                {
                    BaseUrl = "http://indexer.local/"
                }
            };

            _caps = new IndexerCapabilities();

            _caps.Categories.AddCategoryMapping(2000, NewznabStandardCategory.Movies, "Movies");
            _caps.Categories.AddCategoryMapping(2040, NewznabStandardCategory.MoviesHD, "Movies/HD");
            _caps.Categories.AddCategoryMapping(5000, NewznabStandardCategory.TV, "TV");
            _caps.Categories.AddCategoryMapping(5040, NewznabStandardCategory.TVHD, "TV/HD");
            _caps.Categories.AddCategoryMapping(5070, NewznabStandardCategory.TVAnime, "TV/Anime");

            Mocker.GetMock<INewznabCapabilitiesProvider>()
                .Setup(v => v.GetCapabilities(It.IsAny<NewznabSettings>(), It.IsAny<IndexerDefinition>()))
                .Returns(_caps);
        }

        [Test]
        public async Task should_parse_recent_feed_from_torznab_hdaccess_net()
        {
            var recentFeed = ReadAllText(@"Files/Indexers/Torznab/torznab_hdaccess_net.xml");

            Mocker.GetMock<IIndexerHttpClient>()
                .Setup(o => o.ExecuteProxiedAsync(It.Is<HttpRequest>(v => v.Method == HttpMethod.Get), Subject.Definition))
                .ReturnsAsync((HttpRequest r, IndexerDefinition _) => new HttpResponse(r, new HttpHeader(), new CookieCollection(), recentFeed));

            var releases = (await Subject.Fetch(new MovieSearchCriteria())).Releases;

            releases.Should().HaveCount(5);

            releases.First().Should().BeOfType<TorrentInfo>();
            var releaseInfo = releases.First() as TorrentInfo;

            releaseInfo.Title.Should().Be("Better Call Saul S01E05 Alpine Shepherd 1080p NF WEBRip DD5.1 x264");
            releaseInfo.DownloadProtocol.Should().Be(DownloadProtocol.Torrent);
            releaseInfo.DownloadUrl.Should().Be("https://hdaccess.net/download.php?torrent=11515&passkey=123456");
            releaseInfo.InfoUrl.Should().Be("https://hdaccess.net/details.php?id=11515&hit=1");
            releaseInfo.CommentUrl.Should().Be("https://hdaccess.net/details.php?id=11515&hit=1#comments");
            releaseInfo.Indexer.Should().Be(Subject.Definition.Name);
            releaseInfo.PublishDate.Should().Be(DateTime.Parse("2015/03/14 21:10:42"));
            releaseInfo.Size.Should().Be(2538463390);
            releaseInfo.InfoHash.Should().Be("63e07ff523710ca268567dad344ce1e0e6b7e8a3");
            releaseInfo.Seeders.Should().Be(7);
            releaseInfo.Peers.Should().Be(7);
        }

        [Test]
        public async Task should_parse_recent_feed_from_torznab_tpb()
        {
            var recentFeed = ReadAllText(@"Files/Indexers/Torznab/torznab_tpb.xml");

            Mocker.GetMock<IIndexerHttpClient>()
                .Setup(o => o.ExecuteProxiedAsync(It.Is<HttpRequest>(v => v.Method == HttpMethod.Get), Subject.Definition))
                .ReturnsAsync((HttpRequest r, IndexerDefinition _) => new HttpResponse(r, new HttpHeader(), new CookieCollection(), recentFeed));

            var releases = (await Subject.Fetch(new MovieSearchCriteria())).Releases;

            releases.Should().HaveCount(0);
        }

        [Test]
        public async Task should_parse_recent_feed_from_torznab_animetosho()
        {
            var recentFeed = ReadAllText(@"Files/Indexers/Torznab/torznab_animetosho.xml");

            Mocker.GetMock<IIndexerHttpClient>()
                .Setup(o => o.ExecuteProxiedAsync(It.Is<HttpRequest>(v => v.Method == HttpMethod.Get), Subject.Definition))
                .ReturnsAsync((HttpRequest r, IndexerDefinition _) => new HttpResponse(r, new HttpHeader(), new CookieCollection(), recentFeed));

            var releases = (await Subject.Fetch(new MovieSearchCriteria())).Releases;

            releases.Should().HaveCount(2);

            releases.First().Should().BeOfType<TorrentInfo>();
            var releaseInfo = releases.First() as TorrentInfo;

            releaseInfo.Title.Should().Be("[finFAGs]_Frame_Arms_Girl_07_(1280x720_TV_AAC)_[1262B6F7].mkv");
            releaseInfo.DownloadProtocol.Should().Be(DownloadProtocol.Torrent);
            releaseInfo.DownloadUrl.Should().Be("http://storage.localhost/torrents/123451.torrent");
            releaseInfo.InfoUrl.Should().Be("https://localhost/view/finfags-_frame_arms_girl_07_-1280x720_tv_aac-_-1262b6f7-mkv.123451");
            releaseInfo.CommentUrl.Should().Be("https://localhost/view/finfags-_frame_arms_girl_07_-1280x720_tv_aac-_-1262b6f7-mkv.123451");
            releaseInfo.Indexer.Should().Be(Subject.Definition.Name);
            releaseInfo.PublishDate.Should().Be(DateTime.Parse("Wed, 17 May 2017 20:36:06 +0000").ToUniversalTime());
            releaseInfo.Size.Should().Be(316477946);
            releaseInfo.TvdbId.Should().Be(0);
            releaseInfo.TvRageId.Should().Be(0);
            releaseInfo.InfoHash.Should().Be("2d69a861bef5a9f2cdf791b7328e37b7953205e1");
            releaseInfo.Seeders.Should().BeNull();
            releaseInfo.Peers.Should().BeNull();
        }

        [Test]
        public async Task should_parse_recent_feed_from_torznab_morethantv()
        {
            var recentFeed = ReadAllText(@"Files/Indexers/Torznab/torznab_morethantv.xml");

            Mocker.GetMock<IIndexerHttpClient>()
                .Setup(o => o.ExecuteProxiedAsync(It.Is<HttpRequest>(v => v.Method == HttpMethod.Get), Subject.Definition))
                .ReturnsAsync((HttpRequest r, IndexerDefinition _) => new HttpResponse(r, new HttpHeader(), new CookieCollection(), recentFeed));

            var releases = (await Subject.Fetch(new MovieSearchCriteria())).Releases;

            releases.Should().HaveCount(2);

            releases.First().Should().BeOfType<TorrentInfo>();
            var releaseInfo = releases.First() as TorrentInfo;

            releaseInfo.Title.Should().Be("Out of the Past 1947 720p BluRay FLAC2.0 x264-CtrlHD.mkv");
            releaseInfo.DownloadProtocol.Should().Be(DownloadProtocol.Torrent);
            releaseInfo.DownloadUrl.Should().Be("https://www.morethantv.me/torrents.php?action=download&id=(removed)&authkey=(removed)&torrent_pass=(removed)");
            releaseInfo.InfoUrl.Should().Be("https://www.morethantv.me/torrents.php?id=(removed)&torrentid=836164");
            releaseInfo.CommentUrl.Should().Be("https://www.morethantv.me/torrents.php?id=(removed)&torrentid=836164");
            releaseInfo.Indexer.Should().Be(Subject.Definition.Name);
            releaseInfo.PublishDate.Should().Be(DateTime.Parse("Tue, 20 Dec 2022 21:32:17 +0000").ToUniversalTime());
            releaseInfo.Size.Should().Be(5412993028);
            releaseInfo.TvdbId.Should().Be(0);
            releaseInfo.TvRageId.Should().Be(0);
            releaseInfo.InfoHash.Should().Be("(removed)");
            releaseInfo.Seeders.Should().Be(3);
            releaseInfo.Peers.Should().Be(3);
            releaseInfo.Categories.Count.Should().Be(4);
        }

        [Test]
        public void should_use_pagesize_reported_by_caps()
        {
            _caps.LimitsMax = 30;
            _caps.LimitsDefault = 25;

            Subject.PageSize.Should().Be(25);
        }
    }
}
