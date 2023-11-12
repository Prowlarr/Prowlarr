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
    public class ExoticazFixture : CoreTest<ExoticaZ>
    {
        [SetUp]
        public void Setup()
        {
            Subject.Definition = new IndexerDefinition
            {
                Name = "ExoticaZ",
                Settings = new AvistazSettings { Username = "someuser", Password = "somepass", Pid = "somepid" }
            };
        }

        [Test]
        public async Task should_parse_recent_feed_from_ExoticaZ()
        {
            var recentFeed = ReadAllText(@"Files/Indexers/Exoticaz/recentfeed.json");

            Mocker.GetMock<IIndexerHttpClient>()
                .Setup(o => o.ExecuteProxiedAsync(It.Is<HttpRequest>(v => v.Method == HttpMethod.Get), Subject.Definition))
                .Returns<HttpRequest, IndexerDefinition>((r, d) => Task.FromResult(new HttpResponse(r, new HttpHeader { { "Content-Type", "application/json" } }, new CookieCollection(), recentFeed)));

            var releases = (await Subject.Fetch(new MovieSearchCriteria { Categories = new[] { 2000 } })).Releases;

            releases.Should().HaveCount(100);
            releases.First().Should().BeOfType<TorrentInfo>();

            var torrentInfo = releases.First() as TorrentInfo;

            torrentInfo.Title.Should().Be("[SSIS-419] My first experience is Yua Mikami. From the day I lost my virginity, I was devoted to sex.");
            torrentInfo.DownloadProtocol.Should().Be(DownloadProtocol.Torrent);
            torrentInfo.DownloadUrl.Should().Be("https://exoticaz.to/rss/download/(removed)/(removed).torrent");
            torrentInfo.InfoUrl.Should().Be("https://exoticaz.to/torrent/64040-ssis-419-my-first-experience-is-yua-mikami-from-the-day-i-lost-my-virginity-i-was-devoted-to-sex");
            torrentInfo.CommentUrl.Should().BeNullOrEmpty();
            torrentInfo.Indexer.Should().Be(Subject.Definition.Name);
            torrentInfo.PublishDate.Should().Be(DateTime.Parse("2022-06-11 10:04:50"));
            torrentInfo.Size.Should().Be(7085405541);
            torrentInfo.InfoHash.Should().Be("asdjfiasdf54asd7f4a2sdf544asdf");
            torrentInfo.MagnetUrl.Should().Be(null);
            torrentInfo.Peers.Should().Be(33);
            torrentInfo.Seeders.Should().Be(33);
            torrentInfo.Categories.First().Id.Should().Be(6040);
        }
    }
}
