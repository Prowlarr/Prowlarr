using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Http;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Indexers.Definitions.PassThePopcorn;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IndexerTests.PTPTests
{
    [TestFixture]
    public class PTPFixture : CoreTest<PassThePopcorn>
    {
        [SetUp]
        public void Setup()
        {
            Subject.Definition = new IndexerDefinition
            {
                Name = "PTP",
                Settings = new PassThePopcornSettings
                {
                    APIUser = "asdf",
                    APIKey = "sad"
                }
            };
        }

        [TestCase("Files/Indexers/PTP/imdbsearch.json")]
        public async Task should_parse_feed_from_PTP(string fileName)
        {
            var responseJson = ReadAllText(fileName);

            Mocker.GetMock<IIndexerHttpClient>()
                .Setup(o => o.ExecuteProxiedAsync(It.Is<HttpRequest>(v => v.Method == HttpMethod.Get), Subject.Definition))
                  .Returns<HttpRequest, IndexerDefinition>((r, d) => Task.FromResult(new HttpResponse(r, new HttpHeader { ContentType = HttpAccept.Json.Value }, new CookieCollection(), responseJson)));

            var torrents = (await Subject.Fetch(new MovieSearchCriteria())).Releases;

            torrents.Should().HaveCount(293);
            torrents.First().Should().BeOfType<TorrentInfo>();

            var first = torrents.First() as TorrentInfo;

            first.Guid.Should().Be("PassThePopcorn-452135");
            first.Title.Should().Be("The.Night.Of.S01.BluRay.AAC2.0.x264-DEPTH");
            first.DownloadProtocol.Should().Be(DownloadProtocol.Torrent);
            first.DownloadUrl.Should().Be("https://passthepopcorn.me/torrents.php?action=download&id=452135&authkey=00000000000000000000000000000000&torrent_pass=00000000000000000000000000000000");
            first.InfoUrl.Should().Be("https://passthepopcorn.me/torrents.php?id=148131&torrentid=452135");

            //first.PublishDate.Should().Be(DateTime.Parse("2017-04-17T12:13:42+0000").ToUniversalTime()); stupid timezones
            first.Size.Should().Be(2466170624L);
            first.InfoHash.Should().BeNullOrEmpty();
            first.MagnetUrl.Should().BeNullOrEmpty();
            first.Peers.Should().Be(28);
            first.Seeders.Should().Be(26);

            torrents.Any(t => t.IndexerFlags.Contains(IndexerFlag.Scene)).Should().Be(true);
        }
    }
}
