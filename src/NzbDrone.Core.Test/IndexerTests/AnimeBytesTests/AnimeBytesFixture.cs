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

            releases.Should().HaveCount(10);
            releases.First().Should().BeOfType<TorrentInfo>();

            var firstTorrentInfo = releases.First() as TorrentInfo;

            firstTorrentInfo.Title.Should().Be("[GHOST] BLEACH S03 [Blu-ray][MKV][h265 10-bit][1080p][AC3 2.0][Dual Audio][Softsubs (GHOST)]");
            firstTorrentInfo.DownloadProtocol.Should().Be(DownloadProtocol.Torrent);
            firstTorrentInfo.DownloadUrl.Should().Be("https://animebytes.tv/torrent/1031203/download/somepass");
            firstTorrentInfo.InfoUrl.Should().Be("https://animebytes.tv/torrent/1031203/group");
            firstTorrentInfo.Guid.Should().Be("https://animebytes.tv/torrent/1031203/group?nh=F7C73EF631FE269D3A7F10BD12EC99A1");
            firstTorrentInfo.CommentUrl.Should().BeNullOrEmpty();
            firstTorrentInfo.Indexer.Should().Be(Subject.Definition.Name);
            firstTorrentInfo.PublishDate.Should().Be(DateTime.Parse("2023-04-03 03:14:35", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal));
            firstTorrentInfo.Size.Should().Be(24498538059);
            firstTorrentInfo.InfoHash.Should().Be(null);
            firstTorrentInfo.MagnetUrl.Should().Be(null);
            firstTorrentInfo.Peers.Should().Be(2 + 12);
            firstTorrentInfo.Seeders.Should().Be(12);
            firstTorrentInfo.Files.Should().Be(22);
            firstTorrentInfo.MinimumSeedTime.Should().Be(655200);

            var secondTorrentInfo = releases.Skip(2).First() as TorrentInfo;

            secondTorrentInfo.Title.Should().Be("[Polarwindz] Cowboy Bebop: Tengoku no Tobira 2001 [Blu-ray][MKV][h265 10-bit][1080p][Opus 5.1][Softsubs (Polarwindz)]");
            secondTorrentInfo.DownloadProtocol.Should().Be(DownloadProtocol.Torrent);
            secondTorrentInfo.DownloadUrl.Should().Be("https://animebytes.tv/torrent/959397/download/somepass");
            secondTorrentInfo.InfoUrl.Should().Be("https://animebytes.tv/torrent/959397/group");
            secondTorrentInfo.Guid.Should().Be("https://animebytes.tv/torrent/959397/group?nh=D63895DA87A25239C11F9823F46000E1");
            secondTorrentInfo.CommentUrl.Should().BeNullOrEmpty();
            secondTorrentInfo.Indexer.Should().Be(Subject.Definition.Name);
            secondTorrentInfo.PublishDate.Should().Be(DateTime.Parse("2023-04-02 05:00:43", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal));
            secondTorrentInfo.Size.Should().Be(13090646841);
            secondTorrentInfo.InfoHash.Should().Be(null);
            secondTorrentInfo.MagnetUrl.Should().Be(null);
            secondTorrentInfo.Peers.Should().Be(1 + 5);
            secondTorrentInfo.Seeders.Should().Be(5);
            secondTorrentInfo.Files.Should().Be(1);
            secondTorrentInfo.MinimumSeedTime.Should().Be(475200);
        }
    }
}
