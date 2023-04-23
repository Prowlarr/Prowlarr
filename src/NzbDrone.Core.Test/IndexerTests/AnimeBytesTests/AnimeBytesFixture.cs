using System;
using System.Globalization;
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
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IndexerTests.AnimeBytesTests
{
    [TestFixture]
    public class AnimeBytesFixture : CoreTest<AnimeBytes>
    {
        [SetUp]
        public void Setup()
        {
            Subject.Definition = new IndexerDefinition
            {
                Name = "AnimeBytes",
                Settings = new AnimeBytesSettings
                {
                    BaseUrl = "https://animebytes.tv/",
                    Username = "someuser",
                    Passkey = "somepass"
                }
            };
        }

        [Test]
        public async Task should_parse_recent_feed_from_animebytes()
        {
            var recentFeed = ReadAllText(@"Files/Indexers/AnimeBytes/recentfeed.json");

            Mocker.GetMock<IIndexerHttpClient>()
                .Setup(o => o.ExecuteProxiedAsync(It.Is<HttpRequest>(v => v.Method == HttpMethod.Get), Subject.Definition))
                .Returns<HttpRequest, IndexerDefinition>((r, d) => Task.FromResult(new HttpResponse(r, new HttpHeader { { "Content-Type", "application/json" } }, new CookieCollection(), recentFeed)));

            var releases = (await Subject.Fetch(new BasicSearchCriteria { Categories = new[] { 2000, 5000 } })).Releases;

            releases.Should().HaveCount(33);
            releases.First().Should().BeOfType<TorrentInfo>();

            var firstTorrentInfo = releases.ElementAt(2) as TorrentInfo;

            firstTorrentInfo.Title.Should().Be("[SubsPlease] One Piece: The Great Gold Pirate - 1059 [Web][MKV][h264][720p][AAC 2.0][Softsubs (SubsPlease)][Episode 1059]");
            firstTorrentInfo.DownloadProtocol.Should().Be(DownloadProtocol.Torrent);
            firstTorrentInfo.DownloadUrl.Should().Be("https://animebytes.tv/torrent/1043925/download/somepass");
            firstTorrentInfo.InfoUrl.Should().Be("https://animebytes.tv/torrent/1043925/group");
            firstTorrentInfo.Guid.Should().Be("https://animebytes.tv/torrent/1043925/group?nh=0F6BB43603CC07F4C804B9A29139F852");
            firstTorrentInfo.CommentUrl.Should().BeNullOrEmpty();
            firstTorrentInfo.Indexer.Should().Be(Subject.Definition.Name);
            firstTorrentInfo.PublishDate.Should().Be(DateTime.Parse("2023-04-23 02:06:08", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal));
            firstTorrentInfo.Size.Should().Be(743629489);
            firstTorrentInfo.InfoHash.Should().Be(null);
            firstTorrentInfo.MagnetUrl.Should().Be(null);
            firstTorrentInfo.Peers.Should().Be(1 + 114);
            firstTorrentInfo.Seeders.Should().Be(114);
            firstTorrentInfo.Files.Should().Be(1);
            firstTorrentInfo.MinimumSeedTime.Should().Be(259200);

            var secondTorrentInfo = releases.ElementAt(16) as TorrentInfo;

            secondTorrentInfo.Title.Should().Be("[GHOST] BLEACH S03 [Blu-ray][MKV][h265 10-bit][1080p][AC3 2.0][Dual Audio][Softsubs (GHOST)]");
            secondTorrentInfo.DownloadProtocol.Should().Be(DownloadProtocol.Torrent);
            secondTorrentInfo.DownloadUrl.Should().Be("https://animebytes.tv/torrent/1031203/download/somepass");
            secondTorrentInfo.InfoUrl.Should().Be("https://animebytes.tv/torrent/1031203/group");
            secondTorrentInfo.Guid.Should().Be("https://animebytes.tv/torrent/1031203/group?nh=F7C73EF631FE269D3A7F10BD12EC99A1");
            secondTorrentInfo.CommentUrl.Should().BeNullOrEmpty();
            secondTorrentInfo.Indexer.Should().Be(Subject.Definition.Name);
            secondTorrentInfo.PublishDate.Should().Be(DateTime.Parse("2023-04-03 03:14:35", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal));
            secondTorrentInfo.Size.Should().Be(24498538059);
            secondTorrentInfo.InfoHash.Should().Be(null);
            secondTorrentInfo.MagnetUrl.Should().Be(null);
            secondTorrentInfo.Peers.Should().Be(2 + 12);
            secondTorrentInfo.Seeders.Should().Be(12);
            secondTorrentInfo.Files.Should().Be(22);
            secondTorrentInfo.MinimumSeedTime.Should().Be(655200);

            var thirdTorrentInfo = releases.ElementAt(18) as TorrentInfo;

            thirdTorrentInfo.Title.Should().Be("[Polarwindz] Cowboy Bebop: Tengoku no Tobira 2001 [Blu-ray][MKV][h265 10-bit][1080p][Opus 5.1][Softsubs (Polarwindz)]");
            thirdTorrentInfo.DownloadProtocol.Should().Be(DownloadProtocol.Torrent);
            thirdTorrentInfo.DownloadUrl.Should().Be("https://animebytes.tv/torrent/959397/download/somepass");
            thirdTorrentInfo.InfoUrl.Should().Be("https://animebytes.tv/torrent/959397/group");
            thirdTorrentInfo.Guid.Should().Be("https://animebytes.tv/torrent/959397/group?nh=D63895DA87A25239C11F9823F46000E1");
            thirdTorrentInfo.CommentUrl.Should().BeNullOrEmpty();
            thirdTorrentInfo.Indexer.Should().Be(Subject.Definition.Name);
            thirdTorrentInfo.PublishDate.Should().Be(DateTime.Parse("2023-04-02 05:00:43", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal));
            thirdTorrentInfo.Size.Should().Be(13090646841);
            thirdTorrentInfo.InfoHash.Should().Be(null);
            thirdTorrentInfo.MagnetUrl.Should().Be(null);
            thirdTorrentInfo.Peers.Should().Be(1 + 5);
            thirdTorrentInfo.Seeders.Should().Be(5);
            thirdTorrentInfo.Files.Should().Be(1);
            thirdTorrentInfo.MinimumSeedTime.Should().Be(475200);

            var fourthTorrentInfo = releases.ElementAt(3) as TorrentInfo;

            fourthTorrentInfo.Title.Should().Be("[SubsPlease] Dr. STONE: NEW WORLD S03E03 - 03 [Web][MKV][h264][720p][AAC 2.0][Softsubs (SubsPlease)][Episode 3]");
            fourthTorrentInfo.DownloadProtocol.Should().Be(DownloadProtocol.Torrent);
            fourthTorrentInfo.DownloadUrl.Should().Be("https://animebytes.tv/torrent/1041495/download/somepass");
            fourthTorrentInfo.InfoUrl.Should().Be("https://animebytes.tv/torrent/1041495/group");
            fourthTorrentInfo.Guid.Should().Be("https://animebytes.tv/torrent/1041495/group?nh=8B78B0DD3BCC6068BFCD927E4AC674F6");
            fourthTorrentInfo.CommentUrl.Should().BeNullOrEmpty();
            fourthTorrentInfo.Indexer.Should().Be(Subject.Definition.Name);
            fourthTorrentInfo.PublishDate.Should().Be(DateTime.Parse("2023-04-20 14:32:29", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal));
            fourthTorrentInfo.Size.Should().Be(748209543);
            fourthTorrentInfo.InfoHash.Should().Be(null);
            fourthTorrentInfo.MagnetUrl.Should().Be(null);
            fourthTorrentInfo.Peers.Should().Be(3 + 137);
            fourthTorrentInfo.Seeders.Should().Be(137);
            fourthTorrentInfo.Files.Should().Be(1);
            fourthTorrentInfo.MinimumSeedTime.Should().Be(259200);

            var fifthTorrentInfo = releases.ElementAt(23) as TorrentInfo;

            fifthTorrentInfo.Title.Should().Be("[-ZR-] Dr. STONE: STONE WARS S02 [Web][MKV][h264][1080p][AAC 2.0][Dual Audio][Softsubs (-ZR-)]");
            fifthTorrentInfo.DownloadProtocol.Should().Be(DownloadProtocol.Torrent);
            fifthTorrentInfo.DownloadUrl.Should().Be("https://animebytes.tv/torrent/944509/download/somepass");
            fifthTorrentInfo.InfoUrl.Should().Be("https://animebytes.tv/torrent/944509/group");
            fifthTorrentInfo.Guid.Should().Be("https://animebytes.tv/torrent/944509/group?nh=FDCAA1EAB36D7C802F1E4B13DAE5EED7");
            fifthTorrentInfo.CommentUrl.Should().BeNullOrEmpty();
            fifthTorrentInfo.Indexer.Should().Be(Subject.Definition.Name);
            fifthTorrentInfo.PublishDate.Should().Be(DateTime.Parse("2021-06-03 20:30:00", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal));
            fifthTorrentInfo.Size.Should().Be(16611719364);
            fifthTorrentInfo.InfoHash.Should().Be(null);
            fifthTorrentInfo.MagnetUrl.Should().Be(null);
            fifthTorrentInfo.Peers.Should().Be(1 + 31);
            fifthTorrentInfo.Seeders.Should().Be(31);
            fifthTorrentInfo.Files.Should().Be(11);
            fifthTorrentInfo.MinimumSeedTime.Should().Be(529200);

            var sixthTorrentInfo = releases.ElementAt(31) as TorrentInfo;

            sixthTorrentInfo.Title.Should().Be("[HorribleSubs] Dr. STONE S01 [Web][MKV][h264][720p][AAC 2.0][Softsubs (HorribleSubs)]");
            sixthTorrentInfo.DownloadProtocol.Should().Be(DownloadProtocol.Torrent);
            sixthTorrentInfo.DownloadUrl.Should().Be("https://animebytes.tv/torrent/430074/download/somepass");
            sixthTorrentInfo.InfoUrl.Should().Be("https://animebytes.tv/torrent/430074/group");
            sixthTorrentInfo.Guid.Should().Be("https://animebytes.tv/torrent/430074/group?nh=32279E138015D8718B2B4B49AEF64574");
            sixthTorrentInfo.CommentUrl.Should().BeNullOrEmpty();
            sixthTorrentInfo.Indexer.Should().Be(Subject.Definition.Name);
            sixthTorrentInfo.PublishDate.Should().Be(DateTime.Parse("2019-12-13 17:02:48", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal));
            sixthTorrentInfo.Size.Should().Be(16366224176);
            sixthTorrentInfo.InfoHash.Should().Be(null);
            sixthTorrentInfo.MagnetUrl.Should().Be(null);
            sixthTorrentInfo.Peers.Should().Be(1 + 33);
            sixthTorrentInfo.Seeders.Should().Be(33);
            sixthTorrentInfo.Files.Should().Be(24);
            sixthTorrentInfo.MinimumSeedTime.Should().Be(529200);
        }
    }
}
