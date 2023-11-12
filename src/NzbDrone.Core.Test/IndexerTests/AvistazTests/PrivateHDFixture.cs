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
using NzbDrone.Core.Indexers.Definitions.Avistaz;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IndexerTests.AvistazTests
{
    [TestFixture]
    public class PrivateHDFixture : CoreTest<PrivateHD>
    {
        [SetUp]
        public void Setup()
        {
            Subject.Definition = new IndexerDefinition
            {
                Name = "PrivateHD",
                Settings = new AvistazSettings { Username = "someuser", Password = "somepass", Pid = "somepid" }
            };
        }

        [Test]
        public async Task should_parse_recent_feed_from_PrivateHD()
        {
            var recentFeed = ReadAllText(@"Files/Indexers/PrivateHD/recentfeed.json");

            Mocker.GetMock<IIndexerHttpClient>()
                .Setup(o => o.ExecuteProxiedAsync(It.Is<HttpRequest>(v => v.Method == HttpMethod.Get), Subject.Definition))
                .Returns<HttpRequest, IndexerDefinition>((r, d) => Task.FromResult(new HttpResponse(r, new HttpHeader { { "Content-Type", "application/json" } }, new CookieCollection(), recentFeed)));

            var releases = (await Subject.Fetch(new MovieSearchCriteria { Categories = new[] { 2000 } })).Releases;

            releases.Should().HaveCount(100);
            releases.First().Should().BeOfType<TorrentInfo>();

            var torrentInfo = releases.First() as TorrentInfo;

            torrentInfo.Title.Should().Be("Godzilla 2014 2160p UHD BluRay REMUX HDR HEVC Atmos-TRiToN");
            torrentInfo.DownloadProtocol.Should().Be(DownloadProtocol.Torrent);
            torrentInfo.DownloadUrl.Should().Be("https://privatehd.to/rss/download/123456789/78506-a879261d4e6e792402f92401141a21de70d51bf2.torrent");
            torrentInfo.InfoUrl.Should().Be("https://privatehd.to/torrent/78506-godzilla-2014-2160p-uhd-bluray-remux-hdr-hevc-atmos-triton");
            torrentInfo.CommentUrl.Should().BeNullOrEmpty();
            torrentInfo.Indexer.Should().Be(Subject.Definition.Name);
            torrentInfo.PublishDate.Should().Be(DateTime.Parse("2021-03-21 05:24:49"));
            torrentInfo.Size.Should().Be(69914591044);
            torrentInfo.InfoHash.Should().Be("a879261d4e6e792402f92401141a21de70d51bf2");
            torrentInfo.MagnetUrl.Should().Be(null);
            torrentInfo.Peers.Should().Be(32);
            torrentInfo.Seeders.Should().Be(27);
            torrentInfo.ImdbId.Should().Be(831387);
            torrentInfo.TmdbId.Should().Be(124905);
            torrentInfo.TvdbId.Should().Be(0);
        }
    }
}
