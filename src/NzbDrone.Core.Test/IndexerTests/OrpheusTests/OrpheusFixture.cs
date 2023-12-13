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
using NzbDrone.Core.Indexers.Definitions;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IndexerTests.OrpheusTests
{
    [TestFixture]
    public class OrpheusFixture : CoreTest<Orpheus>
    {
        [SetUp]
        public void Setup()
        {
            Subject.Definition = new IndexerDefinition
            {
                Name = "Orpheus",
                Settings = new OrpheusSettings { Apikey = "somekey" }
            };
        }

        [Test]
        public async Task should_parse_recent_feed_from_Orpheus()
        {
            var recentFeed = ReadAllText(@"Files/Indexers/Orpheus/recentfeed.json");

            Mocker.GetMock<IIndexerHttpClient>()
                .Setup(o => o.ExecuteProxiedAsync(It.Is<HttpRequest>(v => v.Method == HttpMethod.Get), Subject.Definition))
                .Returns<HttpRequest, IndexerDefinition>((r, d) => Task.FromResult(new HttpResponse(r, new HttpHeader { { "Content-Type", "application/json" } }, new CookieCollection(), recentFeed)));

            var releases = (await Subject.Fetch(new BasicSearchCriteria { Categories = new[] { 3000 } })).Releases;

            releases.Should().HaveCount(50);
            releases.First().Should().BeOfType<TorrentInfo>();

            var torrentInfo = releases.First() as TorrentInfo;

            torrentInfo.Title.Should().Be("The Beatles - Abbey Road (1969) [Album] [2.0 Mix 2019] [MP3 V2 (VBR) / BD]");
            torrentInfo.DownloadProtocol.Should().Be(DownloadProtocol.Torrent);
            torrentInfo.DownloadUrl.Should().Be("https://orpheus.network/ajax.php?action=download&id=1902448");
            torrentInfo.InfoUrl.Should().Be("https://orpheus.network/torrents.php?id=466&torrentid=1902448");
            torrentInfo.CommentUrl.Should().BeNullOrEmpty();
            torrentInfo.Indexer.Should().Be(Subject.Definition.Name);
            torrentInfo.PublishDate.Should().Be(DateTime.Parse("2022-08-08 2:07:39"));
            torrentInfo.Size.Should().Be(68296866);
            torrentInfo.InfoHash.Should().Be(null);
            torrentInfo.MagnetUrl.Should().Be(null);
            torrentInfo.Peers.Should().Be(0);
            torrentInfo.Seeders.Should().Be(0);
            torrentInfo.Files.Should().Be(18);
            torrentInfo.ImdbId.Should().Be(0);
            torrentInfo.TmdbId.Should().Be(0);
            torrentInfo.TvdbId.Should().Be(0);
            torrentInfo.Languages.Should().HaveCount(0);
            torrentInfo.Subs.Should().HaveCount(0);
            torrentInfo.DownloadVolumeFactor.Should().Be(1);
            torrentInfo.UploadVolumeFactor.Should().Be(1);
        }
    }
}
