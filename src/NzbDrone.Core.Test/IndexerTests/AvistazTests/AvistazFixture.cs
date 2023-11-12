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
    public class AvistazFixture : CoreTest<AvistaZ>
    {
        [SetUp]
        public void Setup()
        {
            Subject.Definition = new IndexerDefinition
            {
                Name = "AvistaZ",
                Settings = new AvistazSettings { Username = "someuser", Password = "somepass", Pid = "somepid" }
            };
        }

        [Test]
        public async Task should_parse_recent_feed_from_AvistaZ()
        {
            var recentFeed = ReadAllText(@"Files/Indexers/Avistaz/recentfeed.json");

            Mocker.GetMock<IIndexerHttpClient>()
                .Setup(o => o.ExecuteProxiedAsync(It.Is<HttpRequest>(v => v.Method == HttpMethod.Get), Subject.Definition))
                .Returns<HttpRequest, IndexerDefinition>((r, d) => Task.FromResult(new HttpResponse(r, new HttpHeader { { "Content-Type", "application/json" } }, new CookieCollection(), recentFeed)));

            var releases = (await Subject.Fetch(new MovieSearchCriteria { Categories = new[] { 2000 } })).Releases;

            releases.Should().HaveCount(100);
            releases.First().Should().BeOfType<TorrentInfo>();

            var torrentInfo = releases.First() as TorrentInfo;

            torrentInfo.Title.Should().Be("JAPAN SINKS: People of Hope 2021 S01E05 720p NF WEB-DL DDP2.0 x264-SEIKEL");
            torrentInfo.DownloadProtocol.Should().Be(DownloadProtocol.Torrent);
            torrentInfo.DownloadUrl.Should().Be("https://avistaz.to/rss/download/(removed)/(removed).torrent");
            torrentInfo.InfoUrl.Should().Be("https://avistaz.to/torrent/187240-japan-sinks-people-of-hope-2021-s01e05-720p-nf-web-dl-ddp20-x264-seikel");
            torrentInfo.CommentUrl.Should().BeNullOrEmpty();
            torrentInfo.Indexer.Should().Be(Subject.Definition.Name);
            torrentInfo.PublishDate.Should().Be(DateTime.Parse("2021-11-14 22:26:21"));
            torrentInfo.Size.Should().Be(935127615);
            torrentInfo.InfoHash.Should().Be("a879261d4e6e792402f92401141a21de70d51bf2");
            torrentInfo.MagnetUrl.Should().Be(null);
            torrentInfo.Peers.Should().Be(20);
            torrentInfo.Seeders.Should().Be(20);
            torrentInfo.ImdbId.Should().Be(15569106);
            torrentInfo.TmdbId.Should().Be(135144);
            torrentInfo.TvdbId.Should().Be(410548);
            torrentInfo.Languages.Should().HaveCount(1);
            torrentInfo.Languages.First().Should().Be("Japanese");
            torrentInfo.Subs.Should().HaveCount(27);
            torrentInfo.Subs.First().Should().Be("Arabic");
        }
    }
}
