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

namespace NzbDrone.Core.Test.IndexerTests.GazelleGamesTests
{
    [TestFixture]
    public class GazelleGamesFixture : CoreTest<GazelleGames>
    {
        [SetUp]
        public void Setup()
        {
            Subject.Definition = new IndexerDefinition
            {
                Name = "GazelleGames",
                Settings = new GazelleGamesSettings { Apikey = "somekey" }
            };
        }

        [Test]
        public async Task should_parse_recent_feed_from_GazelleGames()
        {
            var recentFeed = ReadAllText(@"Files/Indexers/GazelleGames/recentfeed.json");

            Mocker.GetMock<IIndexerHttpClient>()
                .Setup(o => o.ExecuteProxiedAsync(It.Is<HttpRequest>(v => v.Method == HttpMethod.Get), Subject.Definition))
                .Returns<HttpRequest, IndexerDefinition>((r, d) => Task.FromResult(new HttpResponse(r, new HttpHeader { { "Content-Type", "application/json" } }, new CookieCollection(), recentFeed)));

            var releases = (await Subject.Fetch(new BasicSearchCriteria { Categories = new[] { 2000 } })).Releases;

            releases.Should().HaveCount(1462);
            releases.First().Should().BeOfType<TorrentInfo>();

            var torrentInfo = releases.First() as TorrentInfo;

            torrentInfo.Title.Should().Be("Microsoft_Flight_Simulator-HOODLUM (2020) [Windows / Multi-Language / Full ISO]");
            torrentInfo.DownloadProtocol.Should().Be(DownloadProtocol.Torrent);
            torrentInfo.DownloadUrl.Should().Be("https://gazellegames.net/torrents.php?action=download&id=303216&authkey=prowlarr&torrent_pass=");
            torrentInfo.InfoUrl.Should().Be("https://gazellegames.net/torrents.php?id=84781&torrentid=303216");
            torrentInfo.CommentUrl.Should().BeNullOrEmpty();
            torrentInfo.Indexer.Should().Be(Subject.Definition.Name);
            torrentInfo.PublishDate.Should().Be(DateTime.Parse("2022-07-25 06:39:11", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal));
            torrentInfo.Size.Should().Be(80077617780);
            torrentInfo.InfoHash.Should().Be(null);
            torrentInfo.MagnetUrl.Should().Be(null);
            torrentInfo.Peers.Should().Be(383);
            torrentInfo.Seeders.Should().Be(383);
            torrentInfo.ImdbId.Should().Be(0);
            torrentInfo.TmdbId.Should().Be(0);
            torrentInfo.TvdbId.Should().Be(0);
            torrentInfo.Languages.Should().HaveCount(0);
            torrentInfo.Subs.Should().HaveCount(0);
            torrentInfo.DownloadVolumeFactor.Should().Be(1);
            torrentInfo.UploadVolumeFactor.Should().Be(1);
        }

        [Test]
        public async Task should_not_error_if_empty_response()
        {
            var recentFeed = ReadAllText(@"Files/Indexers/GazelleGames/recentfeed-empty.json");

            Mocker.GetMock<IIndexerHttpClient>()
                .Setup(o => o.ExecuteProxiedAsync(It.Is<HttpRequest>(v => v.Method == HttpMethod.Get), Subject.Definition))
                .Returns<HttpRequest, IndexerDefinition>((r, d) => Task.FromResult(new HttpResponse(r, new HttpHeader { { "Content-Type", "application/json" } }, new CookieCollection(), recentFeed)));

            var releases = (await Subject.Fetch(new BasicSearchCriteria { Categories = new[] { 2000 } })).Releases;

            releases.Should().HaveCount(0);
        }
    }
}
