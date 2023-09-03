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
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IndexerTests.SecretCinemaTests
{
    [TestFixture]
    public class SecretCinemaFixture : CoreTest<SecretCinema>
    {
        [SetUp]
        public void Setup()
        {
            Subject.Definition = new IndexerDefinition
            {
                Name = "SecretCinema",
                Settings = new GazelleSettings { Username = "somekey", Password = "somekey" }
            };
        }

        [Test]
        public async Task should_parse_recent_feed_from_SecretCinema()
        {
            var recentFeed = ReadAllText(@"Files/Indexers/SecretCinema/recentfeed.json");

            Mocker.GetMock<IIndexerHttpClient>()
                .Setup(o => o.ExecuteProxiedAsync(It.Is<HttpRequest>(v => v.Method == HttpMethod.Get), Subject.Definition))
                .Returns<HttpRequest, IndexerDefinition>((r, d) => Task.FromResult(new HttpResponse(r, new HttpHeader { { "Content-Type", "application/json" } }, new CookieCollection(), recentFeed)));

            var releases = (await Subject.Fetch(new BasicSearchCriteria { Categories = new[] { 2000 } })).Releases;

            releases.Should().HaveCount(3);
            releases.First().Should().BeOfType<TorrentInfo>();

            var torrentInfo = releases.First() as TorrentInfo;

            torrentInfo.Title.Should().Be("Singin' in the Rain (1952) 2160p");
            torrentInfo.DownloadProtocol.Should().Be(DownloadProtocol.Torrent);
            torrentInfo.DownloadUrl.Should().Be("https://secret-cinema.pw/torrents.php?action=download&id=45068");
            torrentInfo.InfoUrl.Should().Be("https://secret-cinema.pw/torrents.php?id=2497&torrentid=45068");
            torrentInfo.CommentUrl.Should().BeNullOrEmpty();
            torrentInfo.Indexer.Should().Be(Subject.Definition.Name);
            torrentInfo.PublishDate.Should().Be(DateTime.Parse("2022-12-15 17:37:29"));
            torrentInfo.Size.Should().Be(57473058680);
            torrentInfo.InfoHash.Should().Be(null);
            torrentInfo.MagnetUrl.Should().Be(null);
            torrentInfo.Peers.Should().Be(8);
            torrentInfo.Seeders.Should().Be(8);
            torrentInfo.ImdbId.Should().Be(0);
            torrentInfo.TmdbId.Should().Be(0);
            torrentInfo.TvdbId.Should().Be(0);
            torrentInfo.Languages.Should().HaveCount(0);
            torrentInfo.Subs.Should().HaveCount(0);
            torrentInfo.DownloadVolumeFactor.Should().Be(0);
            torrentInfo.UploadVolumeFactor.Should().Be(1);
        }
    }
}
