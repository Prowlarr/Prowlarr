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
using NzbDrone.Core.Indexers.Definitions.Gazelle;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IndexerTests.RedactedTests
{
    [TestFixture]
    public class RedactedFixture : CoreTest<Redacted>
    {
        [SetUp]
        public void Setup()
        {
            Subject.Definition = new IndexerDefinition
            {
                Name = "Redacted",
                Settings = new RedactedSettings { Apikey = "somekey" }
            };
        }

        [Test]
        public async Task should_parse_recent_feed_from_Redacted()
        {
            var recentFeed = ReadAllText(@"Files/Indexers/Redacted/recentfeed.json");

            Mocker.GetMock<IIndexerHttpClient>()
                .Setup(o => o.ExecuteProxiedAsync(It.Is<HttpRequest>(v => v.Method == HttpMethod.Get), Subject.Definition))
                .Returns<HttpRequest, IndexerDefinition>((r, d) => Task.FromResult(new HttpResponse(r, new HttpHeader { { "Content-Type", "application/json" } }, new CookieCollection(), recentFeed)));

            var releases = (await Subject.Fetch(new BasicSearchCriteria { Categories = new[] { 3000 } })).Releases;

            releases.Should().HaveCount(39);
            releases.First().Should().BeOfType<GazelleInfo>();

            var torrentInfo = releases.First() as GazelleInfo;

            torrentInfo.Title.Should().Be("Red Hot Chili Peppers - Californication [1999] [Album] [US / Reissue 2020] [FLAC 24bit Lossless] [Vinyl]");
            torrentInfo.DownloadProtocol.Should().Be(DownloadProtocol.Torrent);
            torrentInfo.DownloadUrl.Should().Be("https://redacted.ch/ajax.php?action=download&id=3892313");
            torrentInfo.InfoUrl.Should().Be("https://redacted.ch/torrents.php?id=16720&torrentid=3892313");
            torrentInfo.CommentUrl.Should().BeNullOrEmpty();
            torrentInfo.Indexer.Should().Be(Subject.Definition.Name);
            torrentInfo.PublishDate.Should().Be(DateTime.Parse("2022-12-17 08:02:35"));
            torrentInfo.Size.Should().Be(1247137236);
            torrentInfo.InfoHash.Should().Be(null);
            torrentInfo.MagnetUrl.Should().Be(null);
            torrentInfo.Peers.Should().Be(4);
            torrentInfo.Seeders.Should().Be(4);
            torrentInfo.Files.Should().Be(23);
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
